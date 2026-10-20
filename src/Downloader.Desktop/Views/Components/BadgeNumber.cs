using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YouTubeDownloader;

/// <summary>Centers the visible numeral, rather than the font's asymmetric line box.</summary>
public sealed class BadgeNumber : Control
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(BadgeNumber),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    private Geometry Numeral() => new FormattedText(Text ?? "", CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
        FontSize, Foreground, VisualTreeHelper.GetDpi(this).PixelsPerDip).BuildGeometry(new Point());

    protected override Size MeasureOverride(Size constraint)
    {
        var bounds = Numeral().Bounds;
        return bounds.IsEmpty ? new Size() : new Size(Math.Ceiling(bounds.Width), Math.Ceiling(bounds.Height));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var number = Numeral();
        var bounds = number.Bounds;
        if (bounds.IsEmpty) return;
        drawingContext.PushTransform(new TranslateTransform(
            (ActualWidth - bounds.Width) / 2 - bounds.X, (ActualHeight - bounds.Height) / 2 - bounds.Y));
        drawingContext.DrawGeometry(Foreground, null, number);
        drawingContext.Pop();
    }
}
