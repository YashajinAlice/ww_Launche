using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WwLauncher.Services;

/// <summary>本機設定（遊戲路徑等）。</summary>
public sealed class AppSettingsService
{
    private const string GamePathKey = "GamePath";
    private const string ThemeKey = "Theme";

    public string GamePath
    {
        get => Read(GamePathKey);
        set => Write(GamePathKey, value ?? string.Empty);
    }

    public string Theme
    {
        get => Read(ThemeKey, "Default");
        set => Write(ThemeKey, value ?? "Default");
    }

    public async Task<string?> PickGameExecutableAsync(Window window)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return null;
        }

        GamePath = file.Path;
        return file.Path;
    }

    private static string Read(string key, string fallback = "")
    {
        try
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            return values.TryGetValue(key, out var raw) && raw is string text
                ? text
                : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static void Write(string key, string value)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }
        catch
        {
            // unpackaged / 權限異常時略過
        }
    }
}
