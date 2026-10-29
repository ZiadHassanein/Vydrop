using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckSubtitleSearchInput()
    {
        // Never show a window, open a popup, capture the mouse or change desktop focus.
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        var view = new SubtitleOptionsView();
        view.Measure(new Size(800, 600));
        view.Arrange(new Rect(0, 0, 800, 600));
        var caption = (ComboBox)view.FindName("CaptionInput");
        caption.ApplyTemplate();
        var search = (TextBox)caption.Template.FindName("CaptionSearchInput", caption);
        Check(search != null && !caption.IsDropDownOpen, "The production subtitle template is tested without opening its native popup");
        var source = new SearchInputSource();
        foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
        {
            view.FlowDirection = direction;
            foreach (var key in new[] { Key.E, Key.A, Key.Space, Key.OemMinus, Key.ImeProcessed })
            {
                var standardInput = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
                { RoutedEvent = Keyboard.KeyDownEvent };
                new TextBox().RaiseEvent(standardInput);
                var input = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
                { RoutedEvent = Keyboard.KeyDownEvent };
                search.RaiseEvent(input);
                Check(input.Handled == standardInput.Handled, $"Subtitle search preserves native text input for {key} in {direction}");
            }
            foreach (string query in new[] { "English", "العربية", "fr-CA" })
            {
                search.Clear();
                var composition = new TextComposition(InputManager.Current, search, query);
                search.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition)
                { RoutedEvent = TextCompositionManager.TextInputEvent });
                Check(search.Text == query, $"Subtitle search accepts composed text '{query}' in {direction}");
            }
        }
        foreach (var key in new[] { Key.Home, Key.End, Key.Left, Key.Right, Key.PageUp, Key.PageDown })
        {
            var input = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key)
            { RoutedEvent = Keyboard.KeyDownEvent };
            search.RaiseEvent(input);
            Check(input.Handled, $"Subtitle search keeps {key} editing inside the field");
        }
        var french = new CaptionChoice("fr-CA", "Français (Canada)");
        Check(SubtitleOptionsView.MatchesCaption(new("en", "English"), "  ENGLISH  "), "Search trims text and ignores case");
        Check(SubtitleOptionsView.MatchesCaption(new("ar", "Arabic"), "العربية"), "Search accepts native Arabic names");
        Check(SubtitleOptionsView.MatchesCaption(french, "francais") && SubtitleOptionsView.MatchesCaption(french, "fr-CA"), "Search accepts accents and regional codes");
        Check(!SubtitleOptionsView.MatchesCaption(french, "no-such-language"), "Unmatched subtitle searches do not match unrelated languages");
        app.Shutdown();
    }

    private sealed class SearchInputSource : PresentationSource
    {
        public override Visual RootVisual { get; set; }
        public override bool IsDisposed => false;
        protected override CompositionTarget GetCompositionTargetCore() => null;
    }
}
