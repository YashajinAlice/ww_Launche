using WwLauncher.Models;

namespace WwLauncher.Services;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 下載更新包並啟動外部套用腳本。呼叫端應接著關閉 UI 並 Environment.Exit。
    /// downloadUrl 須為 zip（內含 WwLauncher.exe 等發佈檔）。
    /// </summary>
    Task ApplyUpdateAndRestartAsync(
        UpdateManifest manifest,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateProgress
{
    public string Stage { get; init; } = string.Empty;
    public double? Percent { get; init; }
}
