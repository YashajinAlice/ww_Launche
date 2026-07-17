using WwLauncher.Models;

namespace WwLauncher.Services;

public interface IAnnouncementService
{
    Task<IReadOnlyList<AnnouncementItem>> GetAnnouncementsAsync(
        AnnouncementCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>取得公告詳情（應用內顯示，不開外部瀏覽器）。</summary>
    Task<AnnouncementDetail> GetDetailAsync(
        AnnouncementItem item,
        CancellationToken cancellationToken = default);
}
