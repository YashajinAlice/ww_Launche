using Microsoft.UI.Xaml.Media.Imaging;

namespace WwLauncher;

internal static class AppAssets
{
    public static string LogoPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Logo.png");
    public static string StoreLogoPath => Path.Combine(AppContext.BaseDirectory, "Assets", "StoreLogo.png");
    public static string IconPath => Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

    public static BitmapImage? TryLoadBitmap(string path, int? decodePixelWidth = null)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var bitmap = new BitmapImage();
            if (decodePixelWidth is int width and > 0)
            {
                bitmap.DecodePixelWidth = width;
            }

            bitmap.UriSource = new Uri(path);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static BitmapImage? LoadLogo(int? decodePixelWidth = null) =>
        TryLoadBitmap(LogoPath, decodePixelWidth) ?? TryLoadBitmap(StoreLogoPath, decodePixelWidth);
}
