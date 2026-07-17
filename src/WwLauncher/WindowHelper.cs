using Microsoft.UI.Xaml;

namespace WwLauncher;

internal static class WindowHelper
{
    private static readonly List<Window> Windows = [];

    public static IReadOnlyList<Window> ActiveWindows => Windows;

    public static void Track(Window window)
    {
        if (Windows.Contains(window))
        {
            return;
        }

        Windows.Add(window);
        window.Closed += (_, _) => Windows.Remove(window);
    }
}
