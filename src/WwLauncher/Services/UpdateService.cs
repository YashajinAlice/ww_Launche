using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using WwLauncher.Models;

namespace WwLauncher.Services;

/// <summary>
/// 版本檢查骨架：
/// 1) 若設定了環境變數 WW_LAUNCHER_UPDATE_URL，則從遠端 JSON 讀取
/// 2) 否則讀取輸出目錄中的 update-manifest.sample.json（本機開發用）
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
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersion();
        var manifest = await LoadManifestAsync(cancellationToken).ConfigureAwait(false);

        if (manifest is null)
        {
            return new UpdateCheckResult
            {
                HasUpdate = false,
                Message = "找不到更新清單。可設定環境變數 WW_LAUNCHER_UPDATE_URL，或使用本機 update-manifest.sample.json。",
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
                Message = $"更新清單版本格式無效：{manifest.Version}",
                Manifest = manifest,
            };
        }

        if (remoteVersion > currentVersion)
        {
            return new UpdateCheckResult
            {
                HasUpdate = true,
                Message = $"發現新版本 {manifest.Version}（目前 {current}）。下載與套用流程尚未實作。",
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

    private async Task<UpdateManifest?> LoadManifestAsync(CancellationToken cancellationToken)
    {
        var remoteUrl = Environment.GetEnvironmentVariable("WW_LAUNCHER_UPDATE_URL");
        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            using var response = await _httpClient.GetAsync(remoteUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<UpdateManifest>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

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
        // 允許 0.1.0-beta 這類字串，只取數字段比較
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
