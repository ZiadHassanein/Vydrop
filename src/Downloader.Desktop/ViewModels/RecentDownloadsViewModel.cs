using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;

public sealed class RecentDownloadsViewModel : ObservableObject
{
    private string error = "";
    public IReadOnlyList<RecentDownloadView> Files { get; }
    public string Heading { get; }
    public string Description { get; }
    public string EmptyHeading { get; }
    public string EmptyDetail { get; }
    public string RevealLabel { get; }
    public string CloseLabel { get; }
    public string Error { get => error; private set { if (SetProperty(ref error, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => Error.Length > 0;
    public bool IsEmpty => Files.Count == 0;
    public IRelayCommand<RecentDownloadView> RevealCommand { get; }

    public RecentDownloadsViewModel(IEnumerable<string> history, Func<string, string> translate, Action<string> reveal)
    {
        Heading = translate("Recent downloads");
        Description = translate("Your last five completed files.");
        EmptyHeading = translate("No downloads yet");
        EmptyDetail = translate("Your completed downloads will appear here.");
        RevealLabel = translate("Show in folder");
        CloseLabel = translate("Close");
        Files = history.Take(5).Select(path => new RecentDownloadView(path, translate)).ToArray();
        RevealCommand = new RelayCommand<RecentDownloadView>(file =>
        {
            Error = "";
            try { reveal(file.FilePath); }
            catch { Error = translate("This file is no longer in its saved location."); }
            finally { file.RefreshAvailability(); RevealCommand.NotifyCanExecuteChanged(); }
        }, file => file != null && file.Available);
    }

    public void RefreshFiles()
    {
        foreach (var file in Files) file.RefreshAvailability();
        RevealCommand.NotifyCanExecuteChanged();
    }
}

public sealed class RecentDownloadView : ObservableObject
{
    private bool available;
    private string details;
    private readonly string extension;
    public string FilePath { get; }
    public string Title { get; }
    public string Details { get => details; private set => SetProperty(ref details, value); }
    public string Folder { get; }
    public string MissingLabel { get; }
    public bool Available { get => available; private set => SetProperty(ref available, value); }
    public bool IsAudio { get; }
    public RecentDownloadView(string path, Func<string, string> translate)
    {
        FilePath = path;
        Title = Path.GetFileNameWithoutExtension(path);
        Folder = Path.GetDirectoryName(path);
        MissingLabel = translate("File moved or deleted");
        extension = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
        IsAudio = !OutputProfiles.IsVideoExtension(extension);
        RefreshAvailability();
    }

    internal void RefreshAvailability()
    {
        try
        {
            var file = new FileInfo(FilePath);
            Available = file.Exists;
            Details = Available ? extension + " · " + YtDlpProvider.Size(file.Length) : extension;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { Available = false; Details = extension; }
    }
}
