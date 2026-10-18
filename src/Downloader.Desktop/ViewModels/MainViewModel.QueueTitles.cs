#nullable enable
namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    private readonly IMediaTitleLookup queueTitleLookup;
    private CancellationTokenSource? queueTitleSession;
    private CancellationTokenSource? queueTitleRequest;
    private Guid? queueTitleEntry;
    private Task queueTitles = Task.CompletedTask;
    internal Task QueueTitlesLoading => queueTitles;

    private void BeginQueueTitles()
    {
        StopQueueTitles();
        queueTitleSession = new CancellationTokenSource();
        foreach (var row in QueueItems) { row.RetryTitle(); row.Refresh(T, Queue.IsRunning); }
        queueTitles = LoadQueueTitlesAsync(queueTitles, queueTitleSession.Token);
    }

    private void StopQueueTitles()
    {
        queueTitleSession?.Cancel();
        queueTitleSession?.Dispose();
        queueTitleSession = null;
    }

    private void ScheduleQueueTitles()
    {
        if (queueTitleSession != null && queueTitles.IsCompleted)
            queueTitles = LoadQueueTitlesAsync(queueTitles, queueTitleSession.Token);
    }

    private async Task LoadQueueTitlesAsync(Task previous, CancellationToken token)
    {
        await previous;
        await Task.Yield();
        try
        {
            while (!token.IsCancellationRequested)
            {
                var row = QueueItems.FirstOrDefault(item => item.NeedsTitle && item.Entry.State == QueueState.Pending);
                if (row == null) return;
                if (IsBusy) { await Task.Delay(150, token); continue; }
                using var request = CancellationTokenSource.CreateLinkedTokenSource(token);
                request.CancelAfter(TimeSpan.FromSeconds(12));
                queueTitleRequest = request;
                queueTitleEntry = row.Id;
                try
                {
                    var title = await queueTitleLookup.FindAsync(row.Url, request.Token);
                    if (!token.IsCancellationRequested && QueueItems.Contains(row)) row.SetPreviewTitle(title);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    if (QueueItems.Contains(row)) row.SetPreviewTitle(null);
                }
                finally
                {
                    queueTitleRequest = null;
                    queueTitleEntry = null;
                }
                await Task.Delay(250, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
}
