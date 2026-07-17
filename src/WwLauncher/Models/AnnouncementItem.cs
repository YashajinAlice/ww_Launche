using System.Text.Json.Serialization;

namespace WwLauncher.Models;

public enum AnnouncementCategory
{
    Game,
    Ours,
}

public sealed class AnnouncementItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>作者公告可帶完整內文（純文字或 HTML）。</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset? PublishedAt { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("bannerUrl")]
    public string BannerUrl { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonIgnore]
    public string PublishedDisplay =>
        PublishedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—";

    [JsonIgnore]
    public string PublishedShort =>
        PublishedAt?.ToLocalTime().ToString("MM-dd") ?? "--";

    [JsonIgnore]
    public string ListTitle => Title;

    [JsonIgnore]
    public string CategoryLabel =>
        Category.Equals("ours", StringComparison.OrdinalIgnoreCase) ? "作者公告" : "遊戲公告";
}

public sealed class AnnouncementDetail
{
    public string Title { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? BannerUrl { get; init; }
    public bool IsHtml { get; init; }
}

public sealed class AnnouncementListResponse
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = "all";

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public List<AnnouncementItem> Items { get; set; } = [];
}

internal sealed class KuroGameAnnouncementDetailDto
{
    [JsonPropertyName("noticeId")]
    public int NoticeId { get; set; }

    [JsonPropertyName("textContent")]
    public string TextContent { get; set; } = string.Empty;

    [JsonPropertyName("textTitle")]
    public string TextTitle { get; set; } = string.Empty;

    [JsonPropertyName("banner")]
    public string Banner { get; set; } = string.Empty;
}
