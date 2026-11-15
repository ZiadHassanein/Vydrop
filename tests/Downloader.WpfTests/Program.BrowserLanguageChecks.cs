using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
                CheckWholeLinkSelection(field, language);
            }
        }
        app.Shutdown();
    }

    private static void CheckWholeLinkSelection(TextBox field, string language)
    {
        // Raise only WPF events on unshown controls; never inject system mouse input.
        MouseButtonEventArgs Click(MouseButton button, int count)
        {
            var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, button)
                { RoutedEvent = Mouse.PreviewMouseDownEvent };
            typeof(MouseButtonEventArgs).GetProperty(nameof(MouseButtonEventArgs.ClickCount)).SetValue(args, count);
            field.RaiseEvent(args);
            return args;
        }
        var binding = field.GetBindingExpression(TextBox.TextProperty);
        foreach (string link in new[] { "https://youtu.be/B0p5SdkBydU?si=HU6d6PS2Vdz42d4p",
            "https://example.com/watch/video-1?title=%D8%B9%D8%B1%D8%A8%D9%8A&token=a.b_c-1#chapter-2" })
        {
            field.Clear();
            field.SetCurrentValue(TextBox.TextProperty, link);
            field.Select(8, 4);
            Check(!Click(MouseButton.Left, 1).Handled && field.SelectionLength == 4,
                $"Single clicks retain normal link editing in {field.Name}/{language}");
            Check(!Click(MouseButton.Right, 2).Handled && field.SelectionLength == 4,
                $"Right clicks retain normal context-menu selection in {field.Name}/{language}");
            Check(Click(MouseButton.Left, 2).Handled && field.SelectionStart == 0 && field.SelectedText == link &&
                ReferenceEquals(binding, field.GetBindingExpression(TextBox.TextProperty)),
                $"Double click selects the entire cleared-and-refilled link, including query and fragment, in {field.Name}/{language}");
        }
        field.Clear();
        Check(!Click(MouseButton.Left, 2).Handled && field.SelectionLength == 0,
            $"Double clicking an empty {field.Name} leaves it ready to type in {language}");
    }
}
