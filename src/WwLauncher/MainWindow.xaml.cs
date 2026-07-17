using System.Reflection;
using Microsoft.UI.Xaml;
using WwLauncher.Services;

namespace WwLauncher;

public sealed partial class MainWindow : Window
{
    private readonly IUpdateService _updateService = new UpdateService();

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = false;

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";

        VersionText.Text = $"目前版本：{version}";
        StatusText.Text = "啟動器骨架就緒。之後可在此接遊戲啟動與更新流程。";
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateProgress.Visibility = Visibility.Visible;
        UpdateProgress.IsIndeterminate = true;
        StatusText.Text = "正在檢查更新…";

        try
        {
            var result = await _updateService.CheckForUpdatesAsync();
            StatusText.Text = result.Message;

            if (result.HasUpdate && result.Manifest is not null)
            {
                StatusText.Text +=
                    $"{Environment.NewLine}遠端版本：{result.Manifest.Version}" +
                    $"{Environment.NewLine}下載網址：{result.Manifest.DownloadUrl}" +
                    $"{Environment.NewLine}說明：{result.Manifest.ReleaseNotes ?? "（無）"}";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"檢查更新失敗：{ex.Message}";
        }
        finally
        {
            UpdateProgress.IsIndeterminate = false;
            UpdateProgress.Visibility = Visibility.Collapsed;
            CheckUpdateButton.IsEnabled = true;
        }
    }
}
