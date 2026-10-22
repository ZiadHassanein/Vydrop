using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace YouTubeDownloader;

internal static class ThemeResources
{
    internal sealed record ContrastPalette(Brush Background, Brush Foreground, Brush Highlight, Brush HighlightText);
    internal static ContrastPalette ContrastOverride { get; set; }
    internal static bool HighContrast => ContrastOverride != null || SystemParameters.HighContrast;
    internal static void ApplyTitleBar(Window window, bool dark)
    {
        int enabled = dark && !HighContrast ? 1 : 0;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        try
        {
            if (DwmSetWindowAttribute(handle, 20, ref enabled, 4) != 0)
                _ = DwmSetWindowAttribute(handle, 19, ref enabled, 4);
        }
        catch (DllNotFoundException) { }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    private static readonly Dictionary<string, (string Light, string Dark)> Palette = new()
    {
        ["Page"] = ("#FCFAFC", "#1C1A20"),
        ["Surface"] = ("#F4EFF2", "#252229"),
        ["Field"] = ("#FFFFFF", "#2D2931"),
        ["Ink"] = ("#2A2029", "#FAF3F7"),
        ["Muted"] = ("#715F6D", "#CCBAC7"),
        ["Accent"] = ("#A91F3B", "#F17B96"),
        ["Action"] = ("#C72D49", "#C72D49"),
        ["OnAccent"] = ("#FFFFFF", "#FFFFFF"),
        ["DownloadActionFill"] = ("#C13046", "#C13046"),
        ["DownloadActionInk"] = ("#FFFFFF", "#FFFFFF"),
        ["DownloadActionProgress"] = ("#651321", "#651321"),
        ["Border"] = ("#8E7D87", "#94828D"),
        ["Hover"] = ("#F0E8EE", "#363039"),
        ["Selected"] = ("#F7E8EF", "#402936"),
        ["Error"] = ("#A12232", "#FFB0B7"),
        ["Divider"] = ("#E2D7DF", "#443B48"),
        ["Success"] = ("#24643E", "#85D1A2"),
        ["Warning"] = ("#81540F", "#EBC17A"),
        ["MediaVideo"] = ("#A91F3B", "#F17B96"),
        ["MediaAudio"] = ("#70519A", "#C7AEF1"),
        ["NavigationIcon"] = ("#EBA6BB", "#EBA6BB"),
        ["Navigation"] = ("#3B1B2C", "#351A2A"),
        ["NavigationInk"] = ("#FFF4FA", "#FFF4FA"),
        ["NavigationMuted"] = ("#DDC2D2", "#DDC2D2"),
        ["NavigationSelected"] = ("#753149", "#642A43"),
        ["NavigationSelectedInk"] = ("#FFFFFF", "#FFFFFF")
    };

    public static void Apply(bool dark, bool animate = false)
    {
        if (animate && !HighContrast && Application.Current.MainWindow is MainWindow { IsVisible: true } window)
        {
            window.ThemeTransition.Run((FrameworkElement)window.Content, window.HeaderContent.ThemeButton, () => Apply(dark));
            return;
        }
        var contrast = ContrastOverride ?? new ContrastPalette(SystemColors.WindowBrush, SystemColors.WindowTextBrush,
            SystemColors.HighlightBrush, SystemColors.HighlightTextBrush);
        Application.Current.Resources["SelectedBorderThickness"] = new Thickness(HighContrast ? 3 : 1);
        foreach (var (key, colors) in Palette)
        {
            Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? colors.Dark : colors.Light));
            if (HighContrast)
                brush = key switch
                {
                    "Ink" or "Muted" or "Error" or "Success" or "Warning" or "MediaVideo" or "MediaAudio" or "NavigationIcon" or "NavigationInk" or "NavigationMuted" => contrast.Foreground,
                    "Accent" or "Action" or "Border" or "DownloadActionFill" => contrast.Highlight,
                    "OnAccent" or "DownloadActionInk" or "DownloadActionProgress" or "NavigationSelectedInk" => contrast.HighlightText,
                    "NavigationSelected" => contrast.Highlight,
                    _ => contrast.Background
                };
            if (brush.CanFreeze) brush.Freeze();
            Application.Current.Resources[key] = brush;
        }
    }
}
