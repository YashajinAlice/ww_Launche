using System.Reflection;
using Microsoft.UI.Xaml;
using WwLauncher.Services;

namespace WwLauncher;

public partial class App : Application
{
    private Window? _window;

    public static new App Current => (App)Application.Current;

    public IUpdateService UpdateService { get; } = new UpdateService();

    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?.Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";

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
