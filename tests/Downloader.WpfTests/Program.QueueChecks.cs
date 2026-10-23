using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using YouTubeDownloader;
internal static partial class Program
{
    private static async Task CheckQueueWindow(Window owner, MainViewModel model, FakeProvider provider, string captures)
    {
        var window = (MainWindow)owner;
        model.Url = ""; model.Folder = System.IO.Path.GetTempPath();
        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
        foreach (bool arabic in new[] { false, true }) foreach (bool dark in new[] { false, true }) foreach (bool compact in new[] { false, true })
        {
            model.SelectLanguageCommand.Execute(arabic ? "ar" : "en"); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            owner.Width = compact ? 820 : 1160; owner.Height = compact ? 500 : 800;
            model.QueueCommand.Execute(null); await Task.Delay(150);
            var view = (QueueComposerView)window.WorkspaceView;
            var viewport = (ScrollViewer)WpfTree.Find(view, "QueueViewport");
            var input = QueueFields(view).Single();
            Check(owner.OwnedWindows.Count == 0 && window.ManagerContent.NavigationSurface.IsEnabled && view.FlowDirection == model.ContentDirection, "Queue composition is an inline section with available navigation in both languages");
            Check(input.IsKeyboardFocusWithin && !input.AcceptsReturn && input.FlowDirection == FlowDirection.LeftToRight, "Queue opens with an accessible single-line URL field");
            InvokeQueueButton((Button)WpfTree.Find(view, "AddLinkFieldButton")); await Task.Delay(80);
            Check(QueueFields(view).Length == 2 && QueueFields(view).Last().IsKeyboardFocusWithin, "Adding a link focuses the new field");
            model.RemoveLinkFieldCommand.Execute(model.QueueLinkInputs.Last());
            input.RaiseEvent(new DataObjectPastingEventArgs(new DataObject(DataFormats.UnicodeText, "https://youtu.be/jNQXAC9IVRw\nhttps://youtu.be/aqz-KE-bpKQ"), false, DataFormats.UnicodeText));
            await Task.Delay(80);
            Check(QueueFields(view).Length == 2, "Multiline paste creates separate URL fields");
            var footer = (FrameworkElement)WpfTree.Find(view, "QueueFooter");
            Check(viewport.ScrollableWidth <= 1 && footer.TranslatePoint(new Point(0, footer.ActualHeight), view).Y <= view.ActualHeight, "The section fits compact windows and keeps actions reachable");
            Capture(owner, System.IO.Path.Combine(captures, $"queue-inline-{arabic}-{dark}-{compact}.png"));
            InvokeQueueButton((Button)WpfTree.Find(view, "AddQueueLinksButton")); await Task.Delay(100);
            Check(model.QueueItems.Count == 2 && !model.HasWorkspacePage && window.ManagerContent.LibraryContent.IsVisible, "Add to queue shows the real Queue list after accepting links");
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
        }
        model.QueueCommand.Execute(null); await Task.Delay(80);
        var composer = (QueueComposerView)window.WorkspaceView;
        var field = QueueFields(composer).Single(); field.Text = "invalid link";
        InvokeQueueButton((Button)WpfTree.Find(composer, "AddQueueLinksButton")); await Task.Delay(80);
        Check(model.QueueLinkInputs.Single().HasError && field.Text == "invalid link" && field.IsKeyboardFocusWithin, "Invalid links remain editable with field-level errors");
        model.LibraryFilterCommand.Execute("all"); model.QueueCommand.Execute(null); await Task.Delay(80);
        Check(QueueFields((QueueComposerView)window.WorkspaceView).Single().Text == "invalid link", "Navigation preserves unfinished queue input");
        model.RemoveLinkFieldCommand.Execute(model.QueueLinkInputs.Single());
        composer = (QueueComposerView)window.WorkspaceView;
        while (model.AddLinkFieldCommand.CanExecute(null)) model.AddLinkFieldCommand.Execute(null);
        model.PasteQueueLinks(model.QueueLinkInputs[0], "https://youtu.be/jNQXAC9IVRw\nhttps://youtu.be/aqz-KE-bpKQ"); await Task.Delay(80);
        Check(model.QueueLinkInputs.Count == 100 && model.QueueLinkInputs[0].HasError && !model.AddLinkFieldCommand.CanExecute(null), "The 100-link limit preserves drafts and explains overflow");
        foreach (var draft in model.QueueLinkInputs.Skip(1).ToArray()) model.RemoveLinkFieldCommand.Execute(draft);
        model.RemoveLinkFieldCommand.Execute(model.QueueLinkInputs[0]);
        model.LibraryFilterCommand.Execute("queue"); owner.Width = 1160; owner.Height = 800; model.SelectLanguageCommand.Execute("en");
    }
    private static TextBox[] QueueFields(FrameworkElement view) => Descendants<TextBox>((ItemsControl)WpfTree.Find(view, "QueueInputFields")).ToArray();
    private static async Task InspectQueueModal(Window owner, MainViewModel model, Func<QueueComposerView, Task> inspect)
    {
        model.QueueCommand.Execute(null);
        await Task.Delay(150);
        var window = (MainWindow)owner;
        var view = (QueueComposerView)window.WorkspaceView;
        try { await inspect(view); }
        finally { if (window.WorkspaceView == view) model.LibraryFilterCommand.Execute("queue"); }
    }
    private static void InvokeQueueButton(Button button) => ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
}
