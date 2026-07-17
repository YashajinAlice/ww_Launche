using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WwLauncher.Views;

namespace WwLauncher;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowHelper.Track(this);

        Title = App.AppDisplayName;
        AppTitleBar.Title = App.Current.AppTitleWithVersion;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1440, 900));

        try
        {
            var ico = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(ico))
            {
                AppWindow.SetIcon(ico);
            }
            else
            {
                AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "StoreLogo.png"));
            }
        }
        catch
        {
            // ignore
        }

        ApplySavedTheme();
    }

    private static void ApplySavedTheme()
    {
        var theme = App.Current.Settings.Theme switch
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

    private void RootNav_Loaded(object sender, RoutedEventArgs e)
    {
        if (RootNav.MenuItems.Count > 0 && RootNav.MenuItems[0] is NavigationViewItem home)
        {
            RootNav.SelectedItem = home;
        }
    }

    private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        RootNav.IsPaneOpen = !RootNav.IsPaneOpen;
    }

    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        AppTitleBar.IsBackButtonEnabled = ContentFrame.CanGoBack;
    }

    private void RootNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        NavigateTag(tag);
    }

    public void NavigateTo(string tag)
    {
        foreach (var menuItem in RootNav.MenuItems.Concat(RootNav.FooterMenuItems))
        {
            if (menuItem is NavigationViewItem item && item.Tag as string == tag)
            {
                RootNav.SelectedItem = item;
                return;
            }
        }
    }

    private void NavigateTag(string tag)
    {
        // 標題列固定「秧寶 版本」，不隨頁面改 Subtitle
        switch (tag)
        {
            case "home":
                ContentFrame.Navigate(typeof(HomePage));
                break;
            case "character":
                ContentFrame.Navigate(typeof(CharacterAnalysisPage));
                break;
            case "gacha":
                ContentFrame.Navigate(typeof(GachaHistoryPage));
                break;
            case "wiki":
                ContentFrame.Navigate(typeof(WikiPage));
                break;
            case "terminal":
                ContentFrame.Navigate(typeof(DataTerminalPage));
                break;
            case "settings":
                ContentFrame.Navigate(typeof(SettingsPage));
                break;
        }
    }
}
