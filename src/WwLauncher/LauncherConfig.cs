namespace WwLauncher;

/// <summary>啟動器執行期設定（可用環境變數覆寫）。</summary>
public static class LauncherConfig
{
    /// <summary>機器人 Web API 根網址。預設對齊 ww_bot WEB_PUBLIC_URL。</summary>
    public static string BotApiBaseUrl =>
        (Environment.GetEnvironmentVariable("WW_LAUNCHER_BOT_API") ?? "https://fulin-net.top")
            .TrimEnd('/');
}
