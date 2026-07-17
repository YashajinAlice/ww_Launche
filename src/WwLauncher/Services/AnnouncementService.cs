using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using WwLauncher.Models;

namespace WwLauncher.Services;

/// <summary>
/// 公告來源：一律打機器人 API。
/// 作者公告由 bot 維護；遊戲公告由 bot 代理 Kuro。
/// 詳情在應用內顯示（遊戲詳情可直連 Kuro JSON）。
/// </summary>
public sealed class AnnouncementService : IAnnouncementService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;

    public AnnouncementService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
    }

    public async Task<IReadOnlyList<AnnouncementItem>> GetAnnouncementsAsync(
        AnnouncementCategory category,
        CancellationToken cancellationToken = default)
    {
        var query = category switch
        {
            AnnouncementCategory.Game => "game",
            AnnouncementCategory.Ours => "ours",
            _ => "all",
        };

        var url = $"{LauncherConfig.BotApiBaseUrl}/api/launcher/announcements?category={query}";
        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var payload = await JsonSerializer
            .DeserializeAsync<AnnouncementListResponse>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        return payload?.Items ?? [];
    }

    public async Task<AnnouncementDetail> GetDetailAsync(
        AnnouncementItem item,
        CancellationToken cancellationToken = default)
    {
        var isGame = item.Category.Equals("game", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(item.Id) && item.Url.Contains("gamenotice", StringComparison.OrdinalIgnoreCase));

        if (!isGame)
        {
            var body = !string.IsNullOrWhiteSpace(item.Content) ? item.Content : item.Summary;
            return new AnnouncementDetail
            {
                Title = item.Title,
                HtmlBody = LooksLikeHtml(body) ? body : PlainTextToHtml(body),
                BannerUrl = string.IsNullOrWhiteSpace(item.BannerUrl) ? null : item.BannerUrl,
                IsHtml = true,
            };
        }

        // 遊戲公告詳情：用 id 重組正確路徑（避免錯誤 contentPrefix 拼接造成 404）
        Exception? lastError = null;
        foreach (var url in BuildGameDetailCandidates(item))
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    lastError = new HttpRequestException(
                        $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).");
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                var dto = await JsonSerializer
                    .DeserializeAsync<KuroGameAnnouncementDetailDto>(stream, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                var title = string.IsNullOrWhiteSpace(dto?.TextTitle) ? item.Title : dto!.TextTitle;
                var html = string.IsNullOrWhiteSpace(dto?.TextContent)
                    ? PlainTextToHtml("（沒有內容）")
                    : dto!.TextContent;

                return new AnnouncementDetail
                {
                    Title = title,
                    HtmlBody = html,
                    BannerUrl = string.IsNullOrWhiteSpace(dto?.Banner) ? item.BannerUrl : dto!.Banner,
                    IsHtml = true,
                };
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new InvalidOperationException("找不到可用的公告詳情位址。");
    }

    private static IEnumerable<string> BuildGameDetailCandidates(AnnouncementItem item)
    {
        const string detailBase =
            "https://aki-gm-resources-back.aki-game.net/gamenotice/content/G153/6eb2a235b30d05efd77bedb5cf60999e/";
        var langs = new[] { "zh-Hant", "zh-Hans", "en" };

        if (!string.IsNullOrWhiteSpace(item.Id))
        {
            foreach (var lang in langs)
            {
                yield return $"{detailBase}{item.Id}/{lang}.json";
            }
        }

        if (!string.IsNullOrWhiteSpace(item.Url)
            && item.Url.Contains(".json", StringComparison.OrdinalIgnoreCase)
            && item.Url.Split("https://", StringSplitOptions.None).Length <= 2)
        {
            yield return item.Url;
        }
    }

    private static bool LooksLikeHtml(string text) =>
        text.Contains('<') && text.Contains('>');

    private static string PlainTextToHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "<p>（沒有內容）</p>";
        }

        var encoded = WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br/>");
        return $"<p>{encoded}</p>";
    }

    public static string WrapDocument(string title, string bodyHtml, string? bannerUrl = null)
    {
        var banner = string.IsNullOrWhiteSpace(bannerUrl)
            ? string.Empty
            : $"<img src=\"{WebUtility.HtmlEncode(bannerUrl)}\" alt=\"\" style=\"max-width:100%;border-radius:8px;margin-bottom:16px;\"/>";

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.Append("<style>");
        sb.Append("html,body{margin:0;padding:0;background:#1b1b1b;color:#f0f0f0;font-family:'Segoe UI',sans-serif;line-height:1.6;}");
        sb.Append("body{padding:16px 18px 24px;}");
        sb.Append("h1{font-size:20px;margin:0 0 12px;font-weight:600;}");
        sb.Append("img{max-width:100%;height:auto;}");
        sb.Append("a{color:#7cb8ff;}");
        sb.Append("p{margin:0 0 12px;}");
        sb.Append("</style></head><body>");
        sb.Append(banner);
        sb.Append("<h1>").Append(WebUtility.HtmlEncode(title)).Append("</h1>");
        sb.Append(bodyHtml);
        sb.Append("</body></html>");
        return sb.ToString();
    }
}
