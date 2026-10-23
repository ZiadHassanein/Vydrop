using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static partial class Program
{
    private static void CaptureVisual(Visual visual, string path, int width = 0, int height = 0)
    {
        var dpi = VisualTreeHelper.GetDpi(visual);
        if (visual is FrameworkElement element && (width <= 0 || height <= 0))
        {
            width = (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX);
            height = (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY);
        }
        var bitmap = new RenderTargetBitmap(width, height, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = System.IO.File.Create(path);
        encoder.Save(stream);
    }
}
