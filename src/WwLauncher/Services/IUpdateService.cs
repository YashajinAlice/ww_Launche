using WwLauncher.Models;

namespace WwLauncher.Services;

public interface IUpdateService
{
    /// <summary>
    /// 檢查是否有新版本。目前支援本機 sample / 遠端 URL（由設定決定）。
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
