using System.Reflection;
using Microsoft.UI.Xaml;
using WwLauncher.Services;

namespace WwLauncher;

public partial class App : Application
{
    private Window? _window;

    public static new App Current => (App)Application.Current;

    public const string AppDisplayName = "秧寶";

    public const string AppAuthor = "YashajinAlice";

    public IUpdateService UpdateService { get; } = new UpdateService();

    public IAnnouncementService AnnouncementService { get; } = new AnnouncementService();

    public AppSettingsService Settings { get; } = new();

    public IGameLaunchService GameLaunch { get; } = new GameLaunchService();

    /// <summary>四段式版本，標題列顯示用（例：0.1.0.0）。</summary>
    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "0.0.0.0";

    /// <summary>標題列文字：秧寶 0.1.0.0</summary>
    public string AppTitleWithVersion => $"{AppDisplayName} {AppVersion}";

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
