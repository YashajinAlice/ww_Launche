using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WwLauncher.Models;
using WwLauncher.Services;

namespace WwLauncher.Views;

public sealed partial class SettingsPage : Page
{
    private bool _ready;
    private bool _checking;
    private UpdateManifest? _latestManifest;

    public SettingsPage()
    {
        InitializeComponent();

        AppNameText.Text = App.AppDisplayName;
        AuthorText.Text = $"作者：{App.AppAuthor}";
        VersionText.Text = $"版本 {App.Current.AppVersion}";
        GamePathBox.Text = App.Current.Settings.GamePath;
        RefreshGamePathStatus();
        UpdateStatusText.Text = "點擊以檢查更新";

        var logo = AppAssets.LoadLogo();
        if (logo is not null)
        {
            AppLogoImage.Source = logo;
        }

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

    private async void CheckUpdateRow_Click(object sender, RoutedEventArgs e)
    {
        if (_checking)
        {
            return;
        }

        _checking = true;
        CheckUpdateRow.IsEnabled = false;
        UpdateStatusText.Text = "正在檢查更新…";
        _latestManifest = null;

        try
        {
            var result = await App.Current.UpdateService.CheckForUpdatesAsync();
            _latestManifest = result.Manifest;

            if (result.HasUpdate && result.Manifest is not null)
            {
                UpdateStatusText.Text = $"發現新版本 {result.Manifest.Version}";
                await ShowUpdateConfirmDialogAsync(result.Manifest);
            }
            else
            {
                UpdateStatusText.Text = "目前已是最新版本";
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "檢查失敗，請稍後再試";
            await ShowSimpleDialogAsync("檢查更新失敗", ex.Message);
        }
        finally
        {
            _checking = false;
            CheckUpdateRow.IsEnabled = true;
        }
    }

    private async Task ShowUpdateConfirmDialogAsync(UpdateManifest manifest)
    {
        var notes = string.IsNullOrWhiteSpace(manifest.ReleaseNotes)
            ? "（沒有更新說明）"
            : manifest.ReleaseNotes!;

        var body = new StackPanel { Spacing = 10 };
        body.Children.Add(new TextBlock
        {
            Text = $"目前版本：{App.Current.AppVersion}",
            Opacity = 0.85,
        });
        body.Children.Add(new TextBlock
        {
            Text = $"新版本：{manifest.Version}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        body.Children.Add(new TextBlock
        {
            Text = "更新內容",
            Margin = new Thickness(0, 8, 0, 0),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        body.Children.Add(new TextBlock
        {
            Text = notes,
            TextWrapping = TextWrapping.WrapWholeWords,
            Opacity = 0.9,
            IsTextSelectionEnabled = true,
        });
        body.Children.Add(new TextBlock
        {
            Text = "確認後會自動下載、套用並重啟啟動器。",
            Margin = new Thickness(0, 8, 0, 0),
            Opacity = 0.75,
            TextWrapping = TextWrapping.WrapWholeWords,
        });

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "確認更新",
            Content = body,
            PrimaryButtonText = "確認更新並重啟",
            CloseButtonText = "稍後",
            DefaultButton = ContentDialogButton.Primary,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        await RunUpdateAsync(manifest);
    }

    private async Task RunUpdateAsync(UpdateManifest manifest)
    {
        var statusText = new TextBlock { Text = "準備中…", TextWrapping = TextWrapping.WrapWholeWords };
        var bar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            IsIndeterminate = true,
            Margin = new Thickness(0, 12, 0, 0),
        };
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(statusText);
        panel.Children.Add(bar);

        var progressDialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "正在更新",
            Content = panel,
            PrimaryButtonText = "請稍候",
            IsPrimaryButtonEnabled = false,
        };

        var progress = new Progress<UpdateProgress>(p =>
        {
            statusText.Text = p.Percent is null
                ? p.Stage
                : $"{p.Stage} {p.Percent:0.#}%";
            if (p.Percent is double percent)
            {
                bar.IsIndeterminate = false;
                bar.Value = percent;
            }
            else
            {
                bar.IsIndeterminate = true;
            }
        });

        _ = progressDialog.ShowAsync();

        try
        {
            await App.Current.UpdateService.ApplyUpdateAndRestartAsync(manifest, progress);

            statusText.Text = "即將重啟…";
            try
            {
                progressDialog.Hide();
            }
            catch
            {
                // ignore
            }

            // 強制結束，讓更新腳本可以覆蓋檔案並重啟
            await Task.Delay(400);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            try
            {
                progressDialog.Hide();
            }
            catch
            {
                // ignore
            }

            UpdateStatusText.Text = "更新失敗";
            await ShowSimpleDialogAsync("更新失敗", ex.Message);
        }
    }

    private async Task ShowSimpleDialogAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "關閉",
        };
        await dialog.ShowAsync();
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
            await ShowSimpleDialogAsync("無法開啟檔案選擇器", "找不到主視窗，請重試。");
            return;
        }

        var path = await App.Current.Settings.PickGameExecutableAsync(host);
        if (!string.IsNullOrWhiteSpace(path))
        {
            GamePathBox.Text = path;
            RefreshGamePathStatus();
        }
    }

    private void ClearGamePath_Click(object sender, RoutedEventArgs e)
    {
        App.Current.Settings.GamePath = string.Empty;
        GamePathBox.Text = string.Empty;
        RefreshGamePathStatus();
    }

    private void VerifyGamePath_Click(object sender, RoutedEventArgs e) => RefreshGamePathStatus();

    private void RefreshGamePathStatus()
    {
        var status = App.Current.GameLaunch.GetStatus(GamePathBox.Text);
        GamePathStatusText.Text = status.IsValid
            ? $"✓ {status.Message}"
            : status.Message;
    }
}