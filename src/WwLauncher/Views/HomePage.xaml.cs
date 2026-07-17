using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using WwLauncher.Models;
using WwLauncher.Services;

namespace WwLauncher.Views;

public sealed partial class HomePage : Page
{
    private const int MaxAnnouncementCount = 5;

    private readonly ObservableCollection<AnnouncementItem> _announcements = [];
    private readonly ObservableCollection<AnnouncementItem> _banners = [];
    private AnnouncementCategory _category = AnnouncementCategory.Game;
    private bool _loaded;
    private bool _webViewReady;
    private bool _launching;

    public HomePage()
    {
        InitializeComponent();
        AnnouncementList.ItemsSource = _announcements;
        BannerFlipView.ItemsSource = _banners;
        Loaded += HomePage_Loaded;
        Unloaded += HomePage_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        RefreshLaunchState();
    }

    private async void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        App.Current.Settings.GamePathChanged += Settings_GamePathChanged;
        RefreshLaunchState();

        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadAnnouncementsAsync();
    }

    private void HomePage_Unloaded(object sender, RoutedEventArgs e)
    {
        App.Current.Settings.GamePathChanged -= Settings_GamePathChanged;
    }

    private void Settings_GamePathChanged(object? sender, string e) => RefreshLaunchState();

    private void RefreshLaunchState()
    {
        var status = App.Current.GameLaunch.GetStatus();
        LaunchButton.IsEnabled = status.IsValid && !_launching;
        ToolTipService.SetToolTip(
            LaunchButton,
            status.IsValid ? $"啟動：{status.Path}" : status.Message);
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_launching)
        {
            return;
        }

        try
        {
            _launching = true;
            RefreshLaunchState();
            App.Current.GameLaunch.Launch();
            AnnouncementStatusBar.Severity = InfoBarSeverity.Success;
            AnnouncementStatusBar.Title = "已啟動遊戲";
            AnnouncementStatusBar.Message = App.Current.Settings.GamePath;
            AnnouncementStatusBar.IsOpen = true;
        }
        catch (Exception ex)
        {
            AnnouncementStatusBar.Severity = InfoBarSeverity.Error;
            AnnouncementStatusBar.Title = "啟動失敗";
            AnnouncementStatusBar.Message = ex.Message;
            AnnouncementStatusBar.IsOpen = true;
        }
        finally
        {
            _launching = false;
            RefreshLaunchState();
        }
    }

    private void VerifyGameFilesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var status = App.Current.GameLaunch.GetStatus();
        AnnouncementStatusBar.Severity = status.IsValid
            ? InfoBarSeverity.Success
            : InfoBarSeverity.Warning;
        AnnouncementStatusBar.Title = status.IsValid ? "遊戲路徑正常" : "遊戲路徑異常";
        AnnouncementStatusBar.Message = string.IsNullOrWhiteSpace(status.Path)
            ? status.Message
            : $"{status.Message}\n{status.Path}";
        AnnouncementStatusBar.IsOpen = true;
        RefreshLaunchState();
    }

    private async void AnnouncementCategoryBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem?.Tag is not string tag)
        {
            return;
        }

        _category = tag == "ours" ? AnnouncementCategory.Ours : AnnouncementCategory.Game;
        await LoadAnnouncementsAsync();
    }

    private async void RefreshAnnouncementsButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAnnouncementsAsync();
    }

    private async void OpenGameFolderMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await App.Current.GameLaunch.OpenGameFolderAsync();
        }
        catch (Exception ex)
        {
            AnnouncementStatusBar.Severity = InfoBarSeverity.Warning;
            AnnouncementStatusBar.Title = "無法開啟遊戲目錄";
            AnnouncementStatusBar.Message = ex.Message;
            AnnouncementStatusBar.IsOpen = true;
        }
    }

    private async Task LoadAnnouncementsAsync()
    {
        RefreshAnnouncementsButton.IsEnabled = false;
        AnnouncementProgress.Visibility = Visibility.Visible;
        AnnouncementStatusBar.IsOpen = false;
        EmptyAnnouncementsText.Visibility = Visibility.Collapsed;

        try
        {
            var items = await App.Current.AnnouncementService.GetAnnouncementsAsync(_category);
            _announcements.Clear();
            foreach (var item in items.Take(MaxAnnouncementCount))
            {
                _announcements.Add(item);
            }

            RefreshBanners(items);

            EmptyAnnouncementsText.Visibility =
                _announcements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _announcements.Clear();
            _banners.Clear();
            EmptyAnnouncementsText.Visibility = Visibility.Visible;
            AnnouncementStatusBar.Severity = InfoBarSeverity.Error;
            AnnouncementStatusBar.Title = "公告載入失敗";
            AnnouncementStatusBar.Message =
                $"{ex.Message}（API：{LauncherConfig.BotApiBaseUrl}/api/launcher/announcements）";
            AnnouncementStatusBar.IsOpen = true;
        }
        finally
        {
            AnnouncementProgress.Visibility = Visibility.Collapsed;
            RefreshAnnouncementsButton.IsEnabled = true;
        }
    }

    private void RefreshBanners(IReadOnlyList<AnnouncementItem> items)
    {
        _banners.Clear();

        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.BannerUrl)).Take(6))
        {
            _banners.Add(item);
        }

        if (_banners.Count == 0)
        {
            foreach (var item in items.Take(3))
            {
                _banners.Add(item);
            }
        }

        if (_banners.Count == 0)
        {
            _banners.Add(new AnnouncementItem
            {
                Title = $"{App.AppDisplayName} {App.Current.AppVersion}",
                Summary = "歡迎回來",
                Content = "歡迎回來",
            });
        }
    }

    private async void AnnouncementList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AnnouncementItem item)
        {
            await ShowAnnouncementDetailAsync(item);
        }
    }

    private async Task ShowAnnouncementDetailAsync(AnnouncementItem item)
    {
        DetailOverlay.Visibility = Visibility.Visible;
        DetailTitleText.Text = item.Title;
        DetailStatusText.Text = "載入中…";
        DetailProgress.Visibility = Visibility.Visible;
        DetailProgress.IsActive = true;

        try
        {
            await EnsureWebViewAsync();
            var detail = await App.Current.AnnouncementService.GetDetailAsync(item);
            DetailTitleText.Text = detail.Title;

            var document = AnnouncementService.WrapDocument(detail.Title, detail.HtmlBody, detail.BannerUrl);
            DetailWebView.NavigateToString(document);
            DetailStatusText.Text = "應用內預覽";
        }
        catch (Exception ex)
        {
            DetailStatusText.Text = $"載入失敗：{ex.Message}";
            try
            {
                await EnsureWebViewAsync();
                var fallback = AnnouncementService.WrapDocument(
                    item.Title,
                    $"<p>{System.Net.WebUtility.HtmlEncode(item.Summary)}</p><p style=\"opacity:.7\">{System.Net.WebUtility.HtmlEncode(ex.Message)}</p>");
                DetailWebView.NavigateToString(fallback);
            }
            catch
            {
                // ignore secondary failure
            }
        }
        finally
        {
            DetailProgress.IsActive = false;
            DetailProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async Task EnsureWebViewAsync()
    {
        if (_webViewReady)
        {
            return;
        }

        await DetailWebView.EnsureCoreWebView2Async();
        _webViewReady = true;
    }

    private void CloseDetailButton_Click(object sender, RoutedEventArgs e) => HideDetailOverlay();

    private void DetailOverlayBackdrop_Tapped(object sender, TappedRoutedEventArgs e) => HideDetailOverlay();

    private void HideDetailOverlay()
    {
        DetailOverlay.Visibility = Visibility.Collapsed;
        try
        {
            DetailWebView.NavigateToString("<html><body style='background:#1b1b1b'></body></html>");
        }
        catch
        {
            // WebView 尚未就緒時略過
        }
    }
}
