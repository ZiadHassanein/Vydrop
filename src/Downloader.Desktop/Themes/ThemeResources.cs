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
        ["Page"] = ("#F7F8FA", "#17191E"),
        ["Surface"] = ("#FFFFFF", "#1F2228"),
        ["SetupFeedbackSurface"] = ("#E8EDF3", "#1F2228"),
        ["SetupFeedbackBorder"] = ("#C4CDD9", "#1F2228"),
        ["Field"] = ("#FFFFFF", "#262A31"),
        ["Ink"] = ("#20242C", "#F1F3F5"),
        ["Muted"] = ("#58616D", "#B7BFCA"),
        ["Accent"] = ("#AD253B", "#F0838C"),
        ["Action"] = ("#C72D49", "#C72D49"),
        ["OnAccent"] = ("#FFFFFF", "#FFFFFF"),
        ["DownloadActionFill"] = ("#C13046", "#C13046"),
        ["DownloadActionInk"] = ("#FFFFFF", "#FFFFFF"),
        ["DownloadActionProgress"] = ("#651321", "#651321"),
        ["Border"] = ("#737E8C", "#84909F"),
        ["Hover"] = ("#E9EDF2", "#2B3038"),
        ["Selected"] = ("#ECEFF3", "#30353D"),
        ["Error"] = ("#A12232", "#F4A3AB"),
        ["Divider"] = ("#DDE2E8", "#363C45"),
        ["Success"] = ("#24643E", "#85D1A2"),
        ["Warning"] = ("#81540F", "#E4BD78"),
        ["MediaVideo"] = ("#58616D", "#B7BFCA"),
        ["MediaAudio"] = ("#58616D", "#B7BFCA"),
        ["NavigationIcon"] = ("#58616D", "#BCC4D0"),
        ["Navigation"] = ("#EEF1F5", "#1B1F27"),
        ["NavigationInk"] = ("#20242C", "#F1F3F5"),
        ["NavigationMuted"] = ("#58616D", "#BCC4D0"),
        ["NavigationSelected"] = ("#DDE4ED", "#343B46"),
        ["NavigationSelectedInk"] = ("#20242C", "#FFFFFF"),
        ["NavigationActiveInk"] = ("#AD253B", "#F58A93")
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
        Application.Current.Resources["SetupFeedbackBorderThickness"] = new Thickness(HighContrast || !dark ? 1 : 0);
        foreach (var (key, colors) in Palette)
        {
            Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? colors.Dark : colors.Light));
            if (HighContrast)
                brush = key switch
                {
                    "Ink" or "Muted" or "Error" or "Success" or "Warning" or "MediaVideo" or "MediaAudio" or "NavigationIcon" or "NavigationInk" or "NavigationMuted" => contrast.Foreground,
                    "Accent" or "Action" or "Border" or "DownloadActionFill" or "SetupFeedbackBorder" or "NavigationActiveInk" => contrast.Highlight,
                    "OnAccent" or "DownloadActionInk" or "DownloadActionProgress" or "NavigationSelectedInk" => contrast.HighlightText,
                    "NavigationSelected" => contrast.Highlight,
                    _ => contrast.Background
                };
            if (brush.CanFreeze) brush.Freeze();
            Application.Current.Resources[key] = brush;
        }
    }
}
