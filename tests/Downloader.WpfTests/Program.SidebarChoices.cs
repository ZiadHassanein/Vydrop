using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CaptureSidebarChoices(MainWindow window, MainViewModel model, string captures)
    {
        MotionPreferences.SetTestOverride(false);
        try
        {
            model.SelectLanguageCommand.Execute("ar");
            model.NewDownloadCommand.Execute(null);
            window.Width = 760;
            window.Height = Math.Min(800, window.CurrentWorkArea().Height - 16);
            await Task.Delay(250); window.UpdateLayout();
            var manager = window.ManagerContent;
            var navigation = manager.NavigationSurface;
            void CaptureSidebar(string name)
            {
                var root = (FrameworkElement)window.Content;
                var dpi = VisualTreeHelper.GetDpi(root);
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(root.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
                bitmap.Render(root);
                var bounds = navigation.TransformToAncestor(root).TransformBounds(new Rect(navigation.RenderSize));
                var crop = new CroppedBitmap(bitmap, new Int32Rect((int)Math.Round(bounds.Left * dpi.DpiScaleX), (int)Math.Round(bounds.Top * dpi.DpiScaleY), (int)Math.Round(bounds.Width * dpi.DpiScaleX), (int)Math.Round(bounds.Height * dpi.DpiScaleY)));
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(crop));
                using var file = File.Create(Path.Combine(captures, name)); encoder.Save(file);
            }
            var frame = (Grid)navigation.Child;
            var buttons = Descendants<ToggleButton>(navigation).ToArray();
            Check(buttons.Length == 8, "Both sidebar options use the same eight navigation actions");
            foreach (var button in buttons)
            {
                button.ApplyTemplate();
                var icon = (System.Windows.Shapes.Path)button.Template.FindName("NavigationGlyph", button);
                icon.Width = icon.Height = 26;
            }
            await Task.Delay(80); window.UpdateLayout();
            CaptureSidebar("B-larger-icons.png");
            foreach (var button in buttons)
            {
                var icon = (System.Windows.Shapes.Path)button.Template.FindName("NavigationGlyph", button);
                icon.Width = icon.Height = 19;
            }
            var oldViewport = frame.Children.OfType<ScrollViewer>().Single();
            var main = (StackPanel)oldViewport.Content;
            var footer = frame.Children.OfType<StackPanel>().Single(panel => Grid.GetRow(panel) == 2);
            var media = main.Children.OfType<TextBlock>().Single();
            var divider = main.Children.OfType<Border>().Single();
            var footerDivider = footer.Children.OfType<Border>().Single();
            var rows = new Grid();
            for (int index = 0; index < 10; index++)
                rows.RowDefinitions.Add(index is 4 or 7 ? new RowDefinition { Height = GridLength.Auto } : new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 54 });
            rows.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            int[] rowIndices = { 0, 1, 2, 3, 5, 6, 8, 9 };
            for (int index = 0; index < buttons.Length; index++)
            {
                ((Panel)buttons[index].Parent).Children.Remove(buttons[index]);
                Grid.SetRow(buttons[index], rowIndices[index]); rows.Children.Add(buttons[index]);
            }
            main.Children.Remove(divider); main.Children.Remove(media); footer.Children.Remove(footerDivider);
            var mediaHeading = new StackPanel();
            divider.Margin = new Thickness(18, 12, 18, 12); media.Margin = new Thickness(24, 0, 24, 8);
            mediaHeading.Children.Add(divider); mediaHeading.Children.Add(media);
            Grid.SetRow(mediaHeading, 4); rows.Children.Add(mediaHeading);
            footerDivider.Margin = new Thickness(18, 12, 18, 12);
            Grid.SetRow(footerDivider, 7); rows.Children.Add(footerDivider);
            frame.Children.Remove(oldViewport); frame.Children.Remove(footer);
            var viewport = new ScrollViewer
            {
                Style = (Style)manager.FindResource("InsetScrollFrame"),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = rows
            };
            BindingOperations.SetBinding(rows, FrameworkElement.MinHeightProperty, new Binding(nameof(ScrollViewer.ViewportHeight)) { Source = viewport });
            Grid.SetRow(viewport, 1); frame.Children.Add(viewport);
            await Task.Delay(120); window.UpdateLayout();
            Check(Math.Abs(rows.ActualHeight - viewport.ViewportHeight) <= 1 && viewport.ScrollableHeight == 0,
                "The spread option uses the available height without scrolling at the pictured window size");
            CaptureSidebar("A-spread-items.png");
            Console.WriteLine("Native sidebar choices saved: " + captures);
        }
        finally { MotionPreferences.SetTestOverride(null); }
    }
}
