using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    public ObservableCollection<QueueLinkInputView> QueueLinkInputs { get; } = new();
    public IRelayCommand AddLinkFieldCommand { get; private set; }
    public IRelayCommand<QueueLinkInputView> RemoveLinkFieldCommand { get; private set; }
    public IRelayCommand<QueueLinkInputView> PasteQueueLinkCommand { get; private set; }
    public IAsyncRelayCommand QueueDownloadCommand { get; private set; }
    public string AddAnotherLinkLabel => T("Add another link");
    public string RemoveLinkLabel => T("Remove link");

    private bool HasDraftLinks => QueueLinkInputs.Any(draft => !string.IsNullOrWhiteSpace(draft.Url));
    private QueueLinkInputView NewQueueLinkInput(QueueLinkInputView copy = null) => new(T, QueueLinkInputChanged, copy?.OutputPreferences ?? OutputRequest());

    private void InitializeQueueInputs()
    {
        AddLinkFieldCommand = new RelayCommand(() =>
        {
            QueueLinkInputs.Add(NewQueueLinkInput(QueueLinkInputs.LastOrDefault()));
            RefreshQueueLinkLabels();
        }, () => QueueLinkInputs.Count < DownloadQueue.Capacity);
        RemoveLinkFieldCommand = new RelayCommand<QueueLinkInputView>(field =>
        {
            if (!QueueLinkInputs.Contains(field)) return;
            field.CancelInspection();
            if (QueueLinkInputs.Count == 1) { field.Url = ""; field.SetError(""); }
            else QueueLinkInputs.Remove(field);
            RefreshQueueLinkLabels();
        });
        PasteQueueLinkCommand = new RelayCommand<QueueLinkInputView>(field => PasteQueueLinks(field, actions.ClipboardText()));
        AddLinksCommand = new RelayCommand(() => { AddLinks(); }, () => !QueueLinkInputs.Any(field => field.HasPendingOutput) && QueueLinkInputs.Any(field => !string.IsNullOrWhiteSpace(field.Url) && field.HasOutputOptions && !field.HasError));
        QueueDownloadCommand = new AsyncRelayCommand(async () =>
        {
            if (HasDraftLinks && !AddLinks()) return;
            if (QueuePrimaryCommand.CanExecute(null)) await QueuePrimaryCommand.ExecuteAsync(null);
        }, () => IsIdle && (HasDraftLinks ? QueueLinkInputs.Where(field => !string.IsNullOrWhiteSpace(field.Url)).All(field => field.HasOutputOptions && !field.HasError) : QueuePrimaryCommand.CanExecute(null)));
        QueueLinkInputs.Add(NewQueueLinkInput());
        RefreshQueueLinkLabels();
    }

    internal void PasteQueueLinks(QueueLinkInputView field, string text)
    {
        if (!QueueLinkInputs.Contains(field) || string.IsNullOrWhiteSpace(text)) return;
        var links = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (QueueLinkInputs.Count + links.Length - 1 > DownloadQueue.Capacity)
        {
            field.SetError("You can enter up to 100 links. Remove a link before adding another.");
            return;
        }
        int index = QueueLinkInputs.IndexOf(field);
        bool retry = field.Url == links[0];
        field.Url = links[0];
        if (retry) field.ResetInspection();
        for (int offset = 1; offset < links.Length; offset++)
        {
            var added = NewQueueLinkInput(field);
            added.Url = links[offset];
            QueueLinkInputs.Insert(index + offset, added);
        }
        RefreshQueueLinkLabels();
    }

    private void RefreshQueueLinkLabels()
    {
        for (int index = 0; index < QueueLinkInputs.Count; index++)
            QueueLinkInputs[index].RefreshLabel(F("Link {0}", index + 1));
        RefreshQueueInputAvailability();
    }

    private void RefreshQueueInputAvailability()
    {
        AddLinksCommand?.NotifyCanExecuteChanged();
        AddLinkFieldCommand?.NotifyCanExecuteChanged();
        QueueDownloadCommand?.NotifyCanExecuteChanged();
        StartQueueFormatChecks();
    }

    private Task queueFormatTask = Task.CompletedTask;
    private CancellationTokenSource queueFormatLifetime;
    private bool drainingQueueFormats;
    private void QueueLinkInputChanged(QueueLinkInputView _) => RefreshQueueInputAvailability();
    private void StartQueueFormatChecks()
    {
        if (browserClosing || drainingQueueFormats || ActiveWorkspace != WorkspacePage.QueueComposer || !IsIdle || !QueueLinkInputs.Any(field => field.NeedsInspection)) return;
        queueFormatLifetime?.Dispose();
        queueFormatLifetime = new CancellationTokenSource();
        drainingQueueFormats = true;
        queueFormatTask = CheckPendingQueueLinksAsync(queueFormatLifetime.Token);
    }
    private async Task CheckPendingQueueLinksAsync(CancellationToken token)
    {
        try
        {
            while (!browserClosing && ActiveWorkspace == WorkspacePage.QueueComposer && IsIdle)
            {
                token.ThrowIfCancellationRequested();
                var field = QueueLinkInputs.FirstOrDefault(item => item.NeedsInspection);
                if (field == null) break;
                var remaining = TimeSpan.FromMilliseconds(450) - (DateTime.UtcNow - field.ChangedAtUtc);
                if (remaining > TimeSpan.Zero) await Task.Delay(remaining, token);
                if (!IsIdle) break;
                if (QueueLinkInputs.Contains(field) && field.NeedsInspection) await InspectQueueLinkAsync(field);
            }
        }
        catch (OperationCanceledException) { }
        finally { drainingQueueFormats = false; RefreshQueueInputAvailability(); }
    }
    private async Task InspectQueueLinkAsync(QueueLinkInputView field)
    {
        string candidate = field.Url.Trim();
        var check = field.BeginInspection();
        try
        {
            var targets = downloads.ResolveTargets(candidate);
            if (targets.VideoUrl == null)
            {
                downloads.Normalize(candidate, true);
                field.UsePlaylistOptions();
            }
            else
            {
                var metadata = await downloads.InspectAsync(targets.VideoUrl, null, check.Token);
                if (QueueLinkInputs.Contains(field) && field.IsCurrentInspection(check, candidate)) field.SetMetadata(metadata);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (field.IsCurrentInspection(check, candidate)) field.SetError(DownloadErrorPresentation.Describe(error));
        }
        finally { field.EndInspection(check); }
    }

    private void CancelQueueLinkInspections()
    {
        queueFormatLifetime?.Cancel();
        foreach (var field in QueueLinkInputs) field.CancelInspection();
    }

    private bool AddLinks()
    {
        if (QueueLinkInputs.Any(field => field.HasPendingOutput)) return false;
        int added = 0;
        foreach (var field in QueueLinkInputs.Where(field => !string.IsNullOrWhiteSpace(field.Url) && field.HasOutputOptions && !field.HasError).ToArray())
        {
            try
            {
                var targets = downloads.ResolveTargets(field.Url.Trim());
                var request = OutputRequest();
                request.Url = targets.VideoUrl ?? targets.PlaylistUrl;
                field.ApplyOutput(request);
                request.PlaylistRequested = targets.VideoUrl == null;
                Queue.Add(request, field.InspectedTitle ?? new Uri(request.Url).Host);
                QueueLinkInputs.Remove(field);
                added++;
            }
            catch (ArgumentException) { field.SetError("Enter a valid video or playlist link."); }
            catch (UriFormatException) { field.SetError("Enter a valid video or playlist link."); }
            catch (InvalidOperationException exception) { field.SetError(exception.Message); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { field.SetError("Queue changes could not be saved. Keep Vydrop open and check free space and folder access."); }
        }
        if (QueueLinkInputs.Count == 0) QueueLinkInputs.Add(NewQueueLinkInput());
        QueueMessage = added > 0 ? F("{0} links added to the queue.", added) : "";
        OnPropertyChanged(nameof(QueueMessage));
        RefreshQueueLinkLabels();
        return !HasDraftLinks;
    }
}
