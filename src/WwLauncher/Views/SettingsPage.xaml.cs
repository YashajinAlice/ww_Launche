using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WwLauncher.Views;

public sealed partial class SettingsPage : Page
{
    private bool _ready;

    public SettingsPage()
    {
        InitializeComponent();
        AboutVersionText.Text = $"WW Launcher {App.Current.AppVersion}";

        ThemeRadioButtons.SelectedIndex = App.Current.RequestedTheme switch
        {
            ApplicationTheme.Light => 1,
            ApplicationTheme.Dark => 2,
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

        // Application.RequestedTheme 只能在啟動前設；執行期改根元素 ElementTheme
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
}
