using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CaptureSettingsPreview(string output)
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        Directory.CreateDirectory(output);
        string root = Path.Combine(AppContext.BaseDirectory, "settings-preview-" + Guid.NewGuid().ToString("N"));
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var model = new MainViewModel(service, new SettingsRepository(root), new DesktopActions(new Window()), new FixtureThumbnailLoader());
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true })
        {
            model.SelectLanguageCommand.Execute(language);
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            var view = new SettingsView { DataContext = model, FontSize = 14 };
            view.SetResourceReference(Control.ForegroundProperty, "Ink");
            var frame = new Border { Padding = new Thickness(24), Child = view };
            frame.SetResourceReference(Border.BackgroundProperty, "Page");
            frame.Measure(new Size(820, 660)); frame.Arrange(new Rect(0, 0, 820, 660)); frame.UpdateLayout();
            CaptureVisual(frame, Path.Combine(output, $"settings-{language}-{(dark ? "dark" : "light")}.png"));
        }
        Console.WriteLine("Rendered Settings in English/Arabic and both themes offscreen without desktop input.");
        app.Shutdown();
    }

    private static void CaptureSetupFeedback(string output)
    {
        // Render the real view offscreen; no window, native focus, clipboard or browser actions.
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        Directory.CreateDirectory(output);
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true })
        {
            ThemeResources.Apply(dark);
            var fixture = new SetupFixture();
            var model = new BrowserSetupViewModel(key => Texts.For(language, key), fixture, language == "ar");
            model.CopyAddressCommand.Execute(null);
            var view = new BrowserIntegrationView
            {
                DataContext = model, FlowDirection = language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                FontFamily = (FontFamily)app.Resources[language == "ar" ? "ArabicFont" : "LatinFont"]
            };
            view.Measure(new Size(820, 690)); view.Arrange(new Rect(0, 0, 820, 690)); view.UpdateLayout();
            CaptureVisual(view, Path.Combine(output, $"setup-feedback-{language}-{(dark ? "dark" : "light")}.png"));
        }
        Console.WriteLine("Rendered English/Arabic browser setup feedback in light and dark without desktop input.");
        app.Shutdown();
    }
}
