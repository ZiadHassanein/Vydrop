using System.Windows;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckBrowserLanguage()
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        string root = System.IO.Path.Combine(AppContext.BaseDirectory, "language-fixture-" + Guid.NewGuid().ToString("N"));
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        // No window is shown or activated; settings/queue writes stay in this fixture.
        var model = new MainViewModel(service, new SettingsRepository(root), new DesktopActions(new Window()), new FixtureThumbnailLoader());
        var form = new DownloadFormView { DataContext = model };
        var queue = new QueueComposerView { DataContext = model };
        foreach (string language in new[] { "en", "ar", "en" })
        {
            model.SelectLanguageCommand.Execute(language);
            var receipt = model.ReceiveBrowserRequest(new BrowserRequest(2, "ping"));
            Check(receipt.Accepted && receipt.Language == language && receipt.Code == "connected",
                "Passive browser health reflects the current app language: " + language);
            Check(model.Queue.Entries.Count == 0 && model.Url == "" && !model.IsBusy,
                "Reading language leaves editor and queue idle");
            foreach (var view in new UserControl[] { form, queue })
            {
                view.Measure(new Size(800, 1100)); view.Arrange(new Rect(0, 0, 800, 1100)); view.UpdateLayout();
                var field = view == form ? form.LinkInput : Descendants<TextBox>(view).Single(box => box.Name == "QueueLinkField");
                field.CaretIndex = 0;
                var caret = field.GetRectFromCharacterIndex(0);
                Check(!caret.IsEmpty && (language == "ar" ? caret.X > field.ActualWidth / 2 : caret.X < field.ActualWidth / 2),
                    $"Empty {field.Name} caret starts at the {(language == "ar" ? "right" : "left")} edge in {language} (x={caret.X}, width={field.ActualWidth})");
            }
        }
        app.Shutdown();
    }
}
