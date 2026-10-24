using System.Text.Json.Serialization;
namespace YouTubeDownloader;
public sealed class DownloadOptions
{
    public string Url, Folder;
    public string QualityKey;
    internal string FormatSelector;
    internal VideoInfo FreshMetadata;
    public bool Audio;
    public int Height = 1080;
    public int AudioBitrate;
    public AudioEncoding AudioEncoding = AudioEncoding.Mp3;
    public VideoContainer VideoContainer;
    [JsonIgnore] public AudioSourceInfo AudioSource;
    public CaptionRequest Captions;
    [JsonIgnore] public MediaSelection Selection;
    public PlaylistInfo Playlist;
    public bool PlaylistItem;
    public bool PlaylistRequested;
    public Guid JobId;
    public string StableOutputName, SourcePage, MediaId;
    public string ThumbnailUrl, Uploader;
    public double Duration;
    public int PartialGeneration;
    public int Connections = 1;
    public long BytesPerSecond;
    public int MaximumAttempts = 2;
    public bool DirectFileAcceleration;
    internal string ResumeDirectory { get; set; }

    public DownloadOptions Snapshot() => new()
    {
        Url = Url, Folder = Folder, QualityKey = QualityKey, FormatSelector = FormatSelector, FreshMetadata = FreshMetadata,
        Audio = Audio, AudioBitrate = AudioBitrate, AudioEncoding = AudioEncoding, VideoContainer = VideoContainer, AudioSource = AudioSource,
        Height = Height, Captions = Captions, Selection = Selection, PlaylistItem = PlaylistItem, PlaylistRequested = PlaylistRequested,
        ResumeDirectory = ResumeDirectory,
        JobId = JobId, StableOutputName = StableOutputName, SourcePage = SourcePage, MediaId = MediaId, PartialGeneration = PartialGeneration,
        ThumbnailUrl = ThumbnailUrl, Uploader = Uploader, Duration = Duration,
        Connections = Connections, BytesPerSecond = BytesPerSecond, MaximumAttempts = MaximumAttempts, DirectFileAcceleration = DirectFileAcceleration,
        Playlist = Playlist == null ? null : new PlaylistInfo
        {
            Id = Playlist.Id, Url = Playlist.Url, Title = Playlist.Title, ThumbnailUrl = Playlist.ThumbnailUrl, IsMix = Playlist.IsMix,
            Entries = Playlist.Entries.Select(e => new PlaylistEntry { Id = e.Id, Title = e.Title, MediaUrl = e.MediaUrl }).ToList()
        }
    };
}
