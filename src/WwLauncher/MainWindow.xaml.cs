using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WwLauncher.Views;

namespace WwLauncher;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowHelper.Track(this);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1120, 720));

        try
        {
            AppWindow.SetIcon("Assets/StoreLogo.png");
        }
        catch
        {
            // 圖示缺失時略過，不影響啟動
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

    private void RootNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            AppTitleBar.Subtitle = "設定";
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        NavigateTag(tag);
    }

    public void NavigateTo(string tag)
    {
        foreach (var menuItem in RootNav.MenuItems)
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
        switch (tag)
        {
            case "home":
                ContentFrame.Navigate(typeof(HomePage));
                AppTitleBar.Subtitle = "首頁";
                break;
            case "updates":
                ContentFrame.Navigate(typeof(UpdatesPage));
                AppTitleBar.Subtitle = "更新";
                break;
        }
    }
}
