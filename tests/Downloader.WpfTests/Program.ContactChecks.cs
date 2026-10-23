using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckContactLinks(MainWindow window, MainViewModel model, string captures)
    {
        model.Url = "";
        model.LibraryFilterCommand.Execute("all");
        Check(WpfTree.Find(window.ManagerContent.NavigationSurface, "ContactButton") == null &&
            !Descendants<TextBlock>(window.ManagerContent.NavigationSurface).Any(text => text.Text == model.ContactLabel),
            "Contact no longer crowds the sidebar or adds a floating footer trigger");
        foreach (string locale in new[] { "en", "ar" })
            foreach (bool dark in new[] { false, true })
                foreach (double width in new[] { 1280d, 760d })
                {
                    model.SelectLanguageCommand.Execute(locale);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    model.SettingsVisible = true;
                    window.Width = width;
                    await Task.Delay(180);
                    window.UpdateLayout();
                    var section = (FrameworkElement)WpfTree.Find(window.SettingsPane, "ContactSection");
                    var heading = (TextBlock)WpfTree.Find(section, "ContactHeading");
                    var actions = (WrapPanel)WpfTree.Find(section, "ContactActions");
                    var links = actions.Children.OfType<Button>().ToArray();
                    var viewport = (ScrollViewer)WpfTree.Find(window, "ContextViewport");
                    section.BringIntoView();
                    await Task.Delay(80);
                    window.UpdateLayout();
                    Check(window.SettingsPane.IsVisible && section.IsVisible &&
                        heading.Text == (locale == "ar" ? "تواصل معي" : "Contact me") &&
                        heading.FlowDirection == model.ContentDirection && section.FlowDirection == model.ContentDirection,
                        "Settings contains a localized contact section whose visible layout follows " + locale);
                    Check(links.Length == 3 && links.All(link => ReferenceEquals(link.Command, model.SocialCommand) &&
                        link.IsVisible && link.IsEnabled && link.ContextMenu == null && link.ActualHeight >= 40) &&
                        links.Select(link => (string)link.CommandParameter).SequenceEqual(new[] { "github", "linkedin", "email" }) &&
                        AutomationProperties.GetName(links[2]) == model.EmailLabel,
                        "GitHub, LinkedIn and Email remain directly available through the existing contact commands");
                    Rect Bounds(FrameworkElement element) => element.TransformToAncestor(viewport).TransformBounds(new Rect(element.RenderSize));
                    var visible = new Rect(0, 0, viewport.ActualWidth, viewport.ActualHeight);
                    Check(viewport.ScrollableWidth == 0 && links.All(link => visible.Contains(Bounds(link))),
                        "Contact links fit the visible Settings workspace without horizontal scrolling");
                    window.Activate();
                    links[0].Focus();
                    Check(links[0].IsKeyboardFocused, "The first contact link can receive keyboard focus");
                    links[0].MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    Check(links[1].IsKeyboardFocused, "Tab advances directly to LinkedIn");
                    links[1].MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    Check(links[2].IsKeyboardFocused, "Tab advances directly to Email without opening a menu");
                    Capture(window, System.IO.Path.Combine(captures, $"contact-{locale}-{(dark ? "dark" : "light")}-{width}.png"));
                    model.SettingsVisible = false;
                    Check(!section.IsVisible, "Leaving Settings removes the contact section from the active workspace");
                }
        model.SelectLanguageCommand.Execute("en");
        window.Width = 1280;
    }
}
