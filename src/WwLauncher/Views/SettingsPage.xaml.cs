using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WwLauncher.Views;

public sealed partial class SettingsPage : Page
{
    private bool _ready;

    public SettingsPage()
    {
        InitializeComponent();

        AppNameText.Text = App.AppDisplayName;
        AuthorText.Text = $"作者：{App.AppAuthor}";
        VersionText.Text = $"版本 {App.Current.AppVersion}";
        GamePathBox.Text = App.Current.Settings.GamePath;

        var theme = App.Current.Settings.Theme;
        ThemeRadioButtons.SelectedIndex = theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0,
        };

        _ready = true;
    }

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || ThemeRadioButtons.SelectedItem is not RadioButton item || item.Tag is not string tag)
        {
            return;
        }

        App.Current.Settings.Theme = tag;
        var theme = tag switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme;
            }
        }
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusBar.IsOpen = true;
        UpdateStatusBar.Severity = InfoBarSeverity.Informational;
        UpdateStatusBar.Title = "檢查中";
        UpdateStatusBar.Message = "正在比對版本…";

        try
        {
            var result = await App.Current.UpdateService.CheckForUpdatesAsync();
            UpdateStatusBar.Severity = result.HasUpdate ? InfoBarSeverity.Success : InfoBarSeverity.Informational;
            UpdateStatusBar.Title = result.HasUpdate ? "有可用更新" : "沒有更新";
            UpdateStatusBar.Message = result.Message;
        }
        catch (Exception ex)
        {
            UpdateStatusBar.Severity = InfoBarSeverity.Error;
            UpdateStatusBar.Title = "檢查失敗";
            UpdateStatusBar.Message = ex.Message;
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    private async void BrowseGamePath_Click(object sender, RoutedEventArgs e)
    {
        Window? host = null;
        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window is MainWindow)
            {
                host = window;
                break;
            }
        }

        if (host is null)
        {
            return;
        }

        var path = await App.Current.Settings.PickGameExecutableAsync(host);
        if (!string.IsNullOrWhiteSpace(path))
        {
            GamePathBox.Text = path;
        }
    }

    private void ClearGamePath_Click(object sender, RoutedEventArgs e)
    {
        App.Current.Settings.GamePath = string.Empty;
        GamePathBox.Text = string.Empty;
    }
}
