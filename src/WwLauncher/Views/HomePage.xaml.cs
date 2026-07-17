using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WwLauncher.Views;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        VersionText.Text = $"目前版本 {App.Current.AppVersion}";
    }

    private void GoUpdates_Click(object sender, RoutedEventArgs e)
    {
        foreach (var window in WindowHelper.ActiveWindows)
        {
            if (window is MainWindow main)
            {
                main.NavigateTo("updates");
                break;
            }
        }
    }
}
