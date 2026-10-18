using System.Net.Http;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private sealed class FixtureTitleLookup : IMediaTitleLookup
    {
        internal Func<string, CancellationToken, Task<string>> Handler = (url, _) =>
            Task.FromResult(url.Contains("jNQXAC9IVRw", StringComparison.Ordinal) ? "An introduction to drawing" : "شرح الرسم خطوة بخطوة");
        internal int Calls;
        public Task<string> FindAsync(string url, CancellationToken token)
        {
            Calls++;
            return Handler(url, token);
        }
    }

    private static async Task CheckQueueTitles(MainWindow owner, MainViewModel model, FixtureTitleLookup lookup, string captures)
    {
        model.Url = "";
        model.Folder = System.IO.Path.GetTempPath();
        var first = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lookup.Handler = async (url, token) =>
        {
            if (!url.Contains("jNQXAC9IVRw", StringComparison.Ordinal)) return "شرح الرسم خطوة بخطوة";
            started.TrySetResult();
            return await first.Task.WaitAsync(token);
        };
        await InspectQueueModal(owner, model, async dialog =>
        {
            model.PasteQueueLinks(model.QueueLinkInputs[0], "https://youtu.be/jNQXAC9IVRw\nhttps://youtu.be/aqz-KE-bpKQ");
            model.AddLinksCommand.Execute(null);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check(model.QueueItems.Count == 2 && model.QueueItems.All(row => row.Title == "Loading video title…"), "Pending rows explain that video titles are loading");
            Check(model.IsIdle && !model.QueueRunning && ((Button)WpfTree.Find(dialog, "QueueStartButton")).IsEnabled, "Title previews never occupy the main download operation or block Start download");
            model.SelectLanguageCommand.Execute("ar");
            Check(model.QueueItems.All(row => row.Title == "جارٍ جلب اسم الفيديو…"), "Loading titles follow an Arabic language switch");
            first.TrySetResult("A recognisable video title — an introduction to drawing");
            await model.QueueTitlesLoading.WaitAsync(TimeSpan.FromSeconds(5));
            Check(model.QueueItems[0].Title.StartsWith("A recognisable video title", StringComparison.Ordinal) && model.QueueItems[1].HasArabicTitle, "Each row displays its own resolved title and detects Arabic lettering");
            Check(model.QueueItems.All(row => row.Entry.State == QueueState.Pending && row.Entry.Result == null && row.Entry.Options.Selection == null), "Preview titles do not start jobs or change the queued format request");
            dialog.UpdateLayout();
            Check(Descendants<TextBlock>(dialog).Any(label => label.Text == model.QueueItems[0].Title) && Descendants<TextBlock>(dialog).Any(label => label.Text == model.QueueItems[1].Title), "Both recognised titles are actually bound into the queue rows");
            ((ScrollViewer)WpfTree.Find(dialog, "QueueViewport")).ScrollToEnd();
            await Task.Delay(75);
            Capture(dialog, System.IO.Path.Combine(captures, "queue-titles-ar.png"));
            model.SelectLanguageCommand.Execute("en");
            Capture(dialog, System.IO.Path.Combine(captures, "queue-titles-en.png"));

            lookup.Handler = (_, _) => Task.FromException<string>(new HttpRequestException("Fixture-only raw failure"));
            model.QueueLinkInputs[0].Url = "https://youtu.be/M7lc1UVf-VE";
            model.AddLinksCommand.Execute(null);
            await model.QueueTitlesLoading.WaitAsync(TimeSpan.FromSeconds(5));
            Check(model.QueueItems.Last().Title == "Video title unavailable" && model.QueueItems.Last().Entry.State == QueueState.Pending, "A title failure keeps the job pending with safe recovery text and its URL");

            var removedStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            bool cancelled = false;
            lookup.Handler = async (_, token) =>
            {
                removedStarted.TrySetResult();
                try { await Task.Delay(Timeout.InfiniteTimeSpan, token); return "Unexpected result"; }
                catch (OperationCanceledException) { cancelled = true; throw; }
            };
            model.QueueLinkInputs[0].Url = "https://youtu.be/9bZkp7q19f0";
            model.AddLinksCommand.Execute(null);
            await removedStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var removed = model.QueueItems.Last();
            model.RemoveQueueCommand.Execute(removed.Id);
            await model.QueueTitlesLoading.WaitAsync(TimeSpan.FromSeconds(5));
            Check(cancelled && !model.QueueItems.Contains(removed), "Removing a row cancels its lookup and a late result cannot resurrect it");
        });
        await model.QueueTitlesLoading.WaitAsync(TimeSpan.FromSeconds(5));

        lookup.Handler = (_, _) => Task.FromResult("Recovered video title");
        int prior = lookup.Calls;
        await InspectQueueModal(owner, model, async dialog =>
        {
            await model.QueueTitlesLoading.WaitAsync(TimeSpan.FromSeconds(5));
            Check(model.QueueItems.Last().Title == "Recovered video title" && lookup.Calls == prior + 1,
                "Reopening retries an unavailable title while reusing successful previews");
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            model.Queue.Add(new DownloadOptions { Url = "https://youtu.be/jNQXAC9IVRw", Folder = model.Folder }, "Already inspected video");
            await model.QueueTitlesLoading.WaitAsync(TimeSpan.FromSeconds(5));
            Check(model.QueueItems.Single().Title == "Already inspected video" && lookup.Calls == prior + 1,
                "A title already inspected in the main window does not cause another lookup");
        });
        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);

        var closingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool closingCancelled = false;
        lookup.Handler = async (_, token) =>
        {
            closingStarted.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); return "Unexpected result"; }
            catch (OperationCanceledException) { closingCancelled = true; throw; }
        };
        await InspectQueueModal(owner, model, async dialog =>
        {
            model.QueueLinkInputs[0].Url = "https://youtu.be/9bZkp7q19f0";
            model.AddLinksCommand.Execute(null);
            await closingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            dialog.Close();
        });
        await model.QueueTitlesLoading.WaitAsync(TimeSpan.FromSeconds(5));
        Check(closingCancelled && model.QueueItems.Single().Entry.State == QueueState.Pending,
            "Closing the queue cancels the title process and retains the pending download");
        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
    }
}
