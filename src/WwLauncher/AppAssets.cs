using Microsoft.UI.Xaml.Media.Imaging;

namespace WwLauncher;

internal static class AppAssets
{
    public static string LogoPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.png");
    public static string StoreLogoPath => Path.Combine(AppContext.BaseDirectory, "Assets", "StoreLogo.png");
    public static string IconPath => Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

    public static BitmapImage? TryLoadBitmap(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return new BitmapImage(new Uri(path));
        }
        catch
        {
            return null;
        }
    }

    public static BitmapImage? LoadLogo() =>
        TryLoadBitmap(LogoPath) ?? TryLoadBitmap(StoreLogoPath);
}
