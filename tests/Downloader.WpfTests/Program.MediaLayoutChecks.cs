using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckMediaLayout(MainWindow window, MainViewModel model, string captures)
    {
        MotionPreferences.SetTestOverride(false);
        try
        {
            FrameworkElement Part(DependencyObject root, string name) => Descendants<FrameworkElement>(root).First(element => element.Name == name);
            var manager = window.ManagerContent;
            var list = manager.DownloadsList;
            var direction = new TextDirectionConverter();
            Check((FlowDirection)direction.Convert("استئناف التنزيل", typeof(FlowDirection), null, CultureInfo.InvariantCulture) == FlowDirection.RightToLeft &&
                (FlowDirection)direction.Convert(@"D:\Videos\تلاوة.mp4", typeof(FlowDirection), null, CultureInfo.InvariantCulture) == FlowDirection.LeftToRight,
                "Arabic hints read right to left while paths keep their literal reading order");
            foreach (bool dark in new[] { false, true }) foreach (double width in new[] { 760d, 1280d }) foreach (string scope in new[] { "queue", "all" })
            {
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                window.Width = width; model.SelectLanguageCommand.Execute("en"); model.LibraryFilterCommand.Execute(scope);
                await Task.Delay(220); window.UpdateLayout();
                var selected = model.LibraryItems.Cast<QueueItemView>().FirstOrDefault(item => item.MissingFile) ?? model.LibraryItems.Cast<QueueItemView>().First();
                model.SelectedLibraryItem = selected; list.ScrollIntoView(selected); window.UpdateLayout();
                (Rect Artwork, string[] Names, double[] Positions, string Font) Snapshot()
                {
                    var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(selected);
                    var content = (FrameworkElement)Part(row, "RowContent");
                    var image = (FrameworkElement)Part(row, "RowArtwork");
                    var actions = (FrameworkElement)Part(row, "RowActions");
                    var buttons = Descendants<Button>(actions).Where(button => button.IsVisible).ToArray();
                    Check(buttons.All(button => button.FlowDirection == FlowDirection.LeftToRight && button.Command != null && Equals(button.CommandParameter, selected.Id)) &&
                        Descendants<System.Windows.Shapes.Path>(actions).All(icon => icon.FlowDirection == FlowDirection.LeftToRight), "Media controls keep their orientation, commands and job identity");
                    return (image.TransformToAncestor(content).TransformBounds(new Rect(image.RenderSize)), buttons.Select(button => button.Name).ToArray(),
                        buttons.Select(button => button.TranslatePoint(new Point(), content).X).ToArray(), ((TextBlock)Part(row, "RowTitle")).FontFamily.Source);
                }
                var baseline = Snapshot();
                foreach (string language in new[] { "ar", "en" })
                {
                    model.SelectLanguageCommand.Execute(language); await Task.Delay(100); window.UpdateLayout();
                    var current = Snapshot();
                    Check(current.Artwork == baseline.Artwork && baseline.Artwork.Left == 0 && current.Names.SequenceEqual(baseline.Names) &&
                        current.Positions.Zip(baseline.Positions).All(pair => Math.Abs(pair.First - pair.Second) <= 1) && current.Font == baseline.Font,
                        $"Thumbnail, button order and title font stay fixed: {scope}/{language}/{dark}/{width}");
                    Check(model.SelectedLibraryItem?.Id == selected.Id && manager.QueueActions.FlowDirection == FlowDirection.LeftToRight,
                        "Language changes preserve the selected job and queue toolbar order");
                    var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(selected);
                    var primary = (Button)Part(row, "RowPrimaryAction");
                    Check((string)primary.ToolTip == selected.PrimaryAction && AutomationProperties.GetName(primary) == selected.PrimaryAction,
                        "Hover and accessible action names follow the selected language");
                    var titleHost = (Border)Part(row, "RowTitleHost");
                    var statusHost = (DockPanel)Part(row, "RowStatusHost");
                    Check(titleHost.ClipToBounds && statusHost.ClipToBounds && titleHost.TranslatePoint(new Point(titleHost.ActualWidth, 0), row).X <= statusHost.TranslatePoint(new Point(), row).X &&
                        ((TextBlock)Part(row, "RowTitle")).TextDecorations.Count == 0,
                        "Long Arabic titles stay readable in their own column without overlapping the file status");
                    var navigationNames = new[] { model.AddDownloadLabel, model.LibraryLabel, model.QueueNavigationLabel, model.HistoryNavigationLabel, model.SettingsLabel };
                    var navigationLabels = Descendants<TextBlock>(manager.NavigationSurface).Where(text => navigationNames.Contains(text.Text)).ToArray();
                    Check(manager.IsNavigationCompact || navigationLabels.All(text => text.TextWrapping == TextWrapping.NoWrap && text.ActualHeight < 28 &&
                        new FormattedText(text.Text, CultureInfo.InvariantCulture, text.FlowDirection, new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch), text.FontSize, text.Foreground, VisualTreeHelper.GetDpi(text).PixelsPerDip).WidthIncludingTrailingWhitespace <= text.ActualWidth + 1),
                        "Wide navigation labels fit on one line; compact navigation uses named icons");
                    var themeGlyph = manager.Header.ThemeGlyph;
                    Check(themeGlyph.Stroke == (Brush)Application.Current.Resources["Ink"], "The theme icon matches the language text and icon color");
                    var details = (Button)Part(row, "RowDetails");
                    Check(details.IsVisible == manager.IsCompact && (!manager.IsCompact || details.IsEnabled), "Each compact row exposes its own small Details control");
                    Capture(window, Path.Combine(captures, $"media-{scope}-{language}-{dark}-{width}.png"));
                    if (manager.IsCompact)
                    {
                        model.SelectedLibraryItem = model.LibraryItems.Cast<QueueItemView>().First(item => item.Id != selected.Id);
                        details.Focus();
                        ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(details).GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
                        await Task.Delay(60);
                        Check(manager.DetailOverlay.IsVisible && !manager.LibraryContent.IsVisible && manager.NavigationSurface.IsEnabled && model.SelectedLibraryItem.Id == selected.Id,
                            "The row Details control opens that exact file in the center workspace without a modal");
                        if (scope == "all") Capture(window, Path.Combine(captures, $"details-{language}-{dark}-{width}.png"));
                        ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(manager.DetailsCloseButton).GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
                        await Task.Delay(60);
                        Check(!manager.DetailOverlay.IsVisible && manager.LibraryContent.IsVisible && (details.IsKeyboardFocusWithin || !window.IsActive && ReferenceEquals(System.Windows.Input.FocusManager.GetFocusedElement(window), details)),
                            "Back returns to the list and restores keyboard focus to the same Details button");
                    }
                    if (width == 1280 && scope == "queue")
                    {
                        var hint = new ToolTip { PlacementTarget = primary, Content = primary.ToolTip };
                        try
                        {
                            hint.IsOpen = true; await Task.Delay(80); hint.UpdateLayout();
                            var surface = (Border)hint.Template.FindName("HintSurface", hint);
                            Check(surface != null && surface.CornerRadius.TopLeft == 8 && hint.MaxWidth == 340 && hint.Padding == new Thickness(12, 8, 12, 8) &&
                                hint.FlowDirection == (language == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight) &&
                                hint.Foreground == (Brush)Application.Current.Resources["Ink"] && hint.FontFamily.Source.Contains(language == "ar" ? "Noto" : "Inter", StringComparison.Ordinal),
                                "Native hints use the shared theme, compact spacing and the correct script direction and font");
                            CaptureVisual(hint, Path.Combine(captures, $"hint-{language}-{dark}.png"));
                            hint.Content = @"D:\Video archive\" + new string('W', 120) + ".mp4";
                            hint.UpdateLayout();
                            Check(hint.ActualWidth <= 340 && hint.ActualHeight > 50 && Descendants<TextBlock>(hint).All(text => text.TextWrapping == TextWrapping.Wrap),
                                "Long filenames wrap inside the tooltip instead of extending off screen");
                        }
                        finally { hint.IsOpen = false; }
                    }
                }
            }
        }
        finally { MotionPreferences.SetTestOverride(null); }
    }
}
