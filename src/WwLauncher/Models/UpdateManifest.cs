using System.Text.Json.Serialization;

namespace WwLauncher.Models;

/// <summary>
/// 遠端更新清單格式。之後可放到 GitHub Releases / 靜態 CDN。
/// </summary>
public sealed class UpdateManifest
{
    [JsonPropertyName("product")]
    public string Product { get; set; } = "WwLauncher";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.0.0";

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("mandatory")]
    public bool Mandatory { get; set; }

    [JsonPropertyName("releaseNotes")]
    public string? ReleaseNotes { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset? PublishedAt { get; set; }
}

public sealed class UpdateCheckResult
{
    public bool HasUpdate { get; init; }
    public string Message { get; init; } = string.Empty;
    public UpdateManifest? Manifest { get; init; }
}
