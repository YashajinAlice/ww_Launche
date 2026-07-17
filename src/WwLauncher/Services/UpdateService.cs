using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using WwLauncher.Models;

namespace WwLauncher.Services;

/// <summary>
/// 版本檢查：
/// 1) 遠端 JSON（預設 GitHub docs/update-manifest.json，可用 WW_LAUNCHER_UPDATE_URL 覆寫）
/// 2) 遠端失敗時回退本機 update-manifest.sample.json
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
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15),
        };
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
