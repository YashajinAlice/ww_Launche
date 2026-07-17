using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WwLauncher.Views;

public sealed partial class UpdatesPage : Page
{
    public UpdatesPage()
    {
        InitializeComponent();
        LocalVersionText.Text = App.Current.AppVersion;
        DetailText.Text = string.Empty;
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateProgress.Visibility = Visibility.Visible;
        StatusBar.Severity = InfoBarSeverity.Informational;
        StatusBar.Title = "檢查中";
        StatusBar.Message = "正在讀取更新清單…";
        DetailText.Text = string.Empty;

        try
        {
            var result = await App.Current.UpdateService.CheckForUpdatesAsync();

            StatusBar.Severity = result.HasUpdate ? InfoBarSeverity.Success : InfoBarSeverity.Informational;
            StatusBar.Title = result.HasUpdate ? "有可用更新" : "沒有更新";
            StatusBar.Message = result.Message;

            if (result.Manifest is not null)
            {
                DetailText.Text =
                    $"遠端版本：{result.Manifest.Version}{Environment.NewLine}" +
                    $"下載網址：{result.Manifest.DownloadUrl}{Environment.NewLine}" +
                    $"強制更新：{(result.Manifest.Mandatory ? "是" : "否")}{Environment.NewLine}" +
                    $"說明：{result.Manifest.ReleaseNotes ?? "（無）"}";
            }
        }
        catch (Exception ex)
        {
            StatusBar.Severity = InfoBarSeverity.Error;
            StatusBar.Title = "檢查失敗";
            StatusBar.Message = ex.Message;
        }
        finally
        {
            UpdateProgress.Visibility = Visibility.Collapsed;
            CheckUpdateButton.IsEnabled = true;
        }
    }
}
