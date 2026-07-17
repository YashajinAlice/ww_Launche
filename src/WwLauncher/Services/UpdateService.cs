using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using WwLauncher.Models;

namespace WwLauncher.Services;

/// <summary>
/// 版本檢查與自動更新（下載 zip → 外部腳本覆蓋 → 重啟）。
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly HttpClient _httpClient;

    public UpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"YangBao/{GetCurrentVersion()}");
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersion();
        string? source = null;
        UpdateManifest? manifest;

        try
        {
            manifest = await LoadRemoteManifestAsync(LauncherConfig.UpdateManifestUrl, cancellationToken)
                .ConfigureAwait(false);
            source = LauncherConfig.UpdateManifestUrl;
        }
        catch
        {
            manifest = await LoadLocalSampleAsync(cancellationToken).ConfigureAwait(false);
            source = "local:update-manifest.sample.json";
        }

        if (manifest is null)
        {
            return new UpdateCheckResult
            {
                HasUpdate = false,
                Message = $"找不到更新清單。已嘗試：{LauncherConfig.UpdateManifestUrl}",
            };
        }

        if (!Version.TryParse(NormalizeVersion(current), out var currentVersion))
        {
            currentVersion = new Version(0, 0, 0);
        }

        if (!Version.TryParse(NormalizeVersion(manifest.Version), out var remoteVersion))
        {
            return new UpdateCheckResult
            {
                HasUpdate = false,
                Message = $"更新清單版本格式無效：{manifest.Version}（來源：{source}）",
                Manifest = manifest,
            };
        }

        if (remoteVersion > currentVersion)
        {
            var notes = string.IsNullOrWhiteSpace(manifest.ReleaseNotes)
                ? string.Empty
                : $"\n說明：{manifest.ReleaseNotes}";

            return new UpdateCheckResult
            {
                HasUpdate = true,
                Message = $"發現新版本 {manifest.Version}（目前 {current}）。{notes}",
                Manifest = manifest,
            };
        }

        return new UpdateCheckResult
        {
            HasUpdate = false,
            Message = $"已是最新版本（{current}）。遠端：{manifest.Version}",
            Manifest = manifest,
        };
    }

    public async Task ApplyUpdateAndRestartAsync(
        UpdateManifest manifest,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
        {
            throw new InvalidOperationException("更新清單缺少 downloadUrl。");
        }

        var workRoot = Path.Combine(Path.GetTempPath(), "YangBaoUpdate", Guid.NewGuid().ToString("N"));
        var zipPath = Path.Combine(workRoot, "package.zip");
        var extractDir = Path.Combine(workRoot, "extract");
        Directory.CreateDirectory(extractDir);

        try
        {
            progress?.Report(new UpdateProgress { Stage = "正在下載更新…", Percent = 0 });
            await DownloadFileAsync(manifest.DownloadUrl, zipPath, progress, cancellationToken)
                .ConfigureAwait(false);

            progress?.Report(new UpdateProgress { Stage = "正在解壓…", Percent = null });
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

            var payloadDir = FindPayloadDirectory(extractDir);
            var exeInPayload = Directory.EnumerateFiles(payloadDir, "WwLauncher.exe", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException("更新包內找不到 WwLauncher.exe。");
            payloadDir = Path.GetDirectoryName(exeInPayload)!;

            var targetDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            const string exeName = "WwLauncher.exe";

            progress?.Report(new UpdateProgress { Stage = "準備套用並重啟…", Percent = null });
            var scriptPath = Path.Combine(workRoot, "apply-and-restart.ps1");
            await File.WriteAllTextAsync(scriptPath, BuildUpdaterScript(), Encoding.UTF8, cancellationToken)
                .ConfigureAwait(false);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" " +
                    $"-TargetDir \"{targetDir}\" " +
                    $"-SourceDir \"{payloadDir}\" " +
                    $"-ExeName \"{exeName}\" " +
                    $"-ProcessId {Environment.ProcessId}",
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            Process.Start(psi);
            progress?.Report(new UpdateProgress { Stage = "即將重啟…", Percent = 100 });
            App.Current.Exit();
        }
        catch
        {
            try
            {
                if (Directory.Exists(workRoot))
                {
                    Directory.Delete(workRoot, recursive: true);
                }
            }
            catch
            {
                // ignore
            }

            throw;
        }
    }

    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<UpdateProgress>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "下載位址不是更新包（收到 HTML）。請將 downloadUrl 設為 zip 直連。");
        }

        var total = response.Content.Headers.ContentLength;
        await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var local = File.Create(destinationPath);

        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await remote.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                   .ConfigureAwait(false)) > 0)
        {
            await local.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            readTotal += read;
            if (total is > 0)
            {
                progress?.Report(new UpdateProgress
                {
                    Stage = "正在下載更新…",
                    Percent = Math.Round(readTotal * 100.0 / total.Value, 1),
                });
            }
        }
    }

    private static string FindPayloadDirectory(string extractDir)
    {
        var dirs = Directory.GetDirectories(extractDir);
        var files = Directory.GetFiles(extractDir);
        if (files.Length == 0 && dirs.Length == 1)
        {
            return dirs[0];
        }

        return extractDir;
    }

    private static string BuildUpdaterScript() => """
param(
  [Parameter(Mandatory = $true)][string]$TargetDir,
  [Parameter(Mandatory = $true)][string]$SourceDir,
  [Parameter(Mandatory = $true)][string]$ExeName,
  [Parameter(Mandatory = $true)][int]$ProcessId
)

$ErrorActionPreference = 'Stop'
try {
  $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
  if ($null -ne $proc) {
    Wait-Process -Id $ProcessId -Timeout 60 -ErrorAction SilentlyContinue
  }
  Start-Sleep -Seconds 1

  if (-not (Test-Path -LiteralPath $SourceDir)) {
    throw "Source not found: $SourceDir"
  }
  if (-not (Test-Path -LiteralPath $TargetDir)) {
    New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
  }

  & robocopy $SourceDir $TargetDir /E /IS /IT /R:2 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
  $code = $LASTEXITCODE
  if ($code -ge 8) {
    throw "robocopy failed with code $code"
  }

  $exePath = Join-Path $TargetDir $ExeName
  if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Updated exe missing: $exePath"
  }

  Start-Process -FilePath $exePath -WorkingDirectory $TargetDir
}
catch {
  $log = Join-Path $env:TEMP 'YangBaoUpdate-error.log'
  $_ | Out-File -FilePath $log -Encoding utf8
}
""";

    private async Task<UpdateManifest?> LoadRemoteManifestAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<UpdateManifest?> LoadLocalSampleAsync(CancellationToken cancellationToken)
    {
        var localPath = Path.Combine(AppContext.BaseDirectory, "update-manifest.sample.json");
        if (!File.Exists(localPath))
        {
            return null;
        }

        await using var file = File.OpenRead(localPath);
        return await JsonSerializer.DeserializeAsync<UpdateManifest>(file, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?.Split('+')[0]
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "0.0.0";
    }

    private static string NormalizeVersion(string version)
    {
        var core = version.Split('-', '+')[0].Trim();
        var parts = core.Split('.');
        while (parts.Length < 3)
        {
            core += ".0";
            parts = core.Split('.');
        }

        return string.Join('.', parts.Take(4));
    }
}
