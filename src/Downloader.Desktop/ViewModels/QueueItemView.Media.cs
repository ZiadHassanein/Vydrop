using System.Globalization;
using System.Windows.Media;

namespace YouTubeDownloader;

public sealed partial class QueueItemView
{
    private ImageSource artwork;
    internal string ArtworkAddress { get; set; }
    public ImageSource Artwork { get => artwork; internal set => SetProperty(ref artwork, value); }
    public bool IsHighContrast => ThemeResources.HighContrast;
    public string SourceName { get { try { return new Uri(Entry.Options.SourcePage ?? Url).Host.Replace("www.", "", StringComparison.Ordinal); } catch (UriFormatException) { return ""; } } }
    public bool IsAudio => Entry.Options.Audio;
    public bool IsComplete => Entry.State == QueueState.Completed;
    public bool MissingFile => IsComplete && !File.Exists(OutputFile);
    public bool HasLocalFile => IsComplete && !MissingFile;
    public string OutputFile => Entry.Result?.FilePath ?? Entry.Result?.Files.LastOrDefault();
    public string SaveFolder => Entry.Options.Folder;
    public string OutputDescription { get; private set; }
    public string TransferSummary { get; private set; }
    public string Speed { get; private set; }
    public string Remaining { get; private set; }
    public string MediaStatus { get; private set; }
    public string PrimaryAction { get; private set; }
    public string CaptionSummary { get; private set; }
    public string RowDescription { get; private set; }
    public string RowStatus => IsActive && !IsPreparing || Entry.State is QueueState.Paused or QueueState.Interrupted
        ? MediaStatus + " · " + Percent.ToString("0", CultureInfo.CurrentCulture) + "%" : MediaStatus;
    public bool HasTransferProgress => IsActive || Entry.State is QueueState.Paused or QueueState.Interrupted;
    public Geometry ActionIcon => (Geometry)System.Windows.Application.Current.Resources[IsComplete ? "OpenFolderIcon" : IsActive ? "ManagerPauseIcon" : "ManagerPlayIcon"];
    public Geometry StatusIcon => (Geometry)System.Windows.Application.Current.Resources[MissingFile || Entry.State == QueueState.Failed ? "AlertIcon" : IsComplete ? "CheckIcon" : IsActive ? "DownloadIcon" : Entry.State is QueueState.Paused or QueueState.Interrupted ? "ManagerPauseIcon" : "ManagerClockIcon"];
    public string SourceKind => SourceName switch
    {
        "youtube.com" or "youtu.be" or "m.youtube.com" => "youtube",
        "vimeo.com" or "player.vimeo.com" => "vimeo",
        "soundcloud.com" => "soundcloud",
        "linkedin.com" => "linkedin",
        "facebook.com" or "fb.watch" or "m.facebook.com" => "facebook",
        _ => "web"
    };
    public string Duration => Entry.Options.Duration is > 0 and < 8640000 ? TimeSpan.FromSeconds(Entry.Options.Duration).ToString(Entry.Options.Duration >= 3600 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture) : "";
    public Brush StateBrush => (Brush)System.Windows.Application.Current.Resources[MissingFile || Entry.State == QueueState.Failed ? "Error" : IsComplete ? "Success" : IsActive || Entry.State is QueueState.Paused or QueueState.Interrupted ? "Accent" : "Muted"];
    public Geometry MediaIcon => (Geometry)System.Windows.Application.Current.Resources[IsAudio ? "AudioIcon" : "VideoIcon"];
    internal void RefreshMedia(Func<string, string> t)
    {
        var options = Entry.Options;
        string format = options.Audio ? OutputProfiles.AudioExtension(options.AudioEncoding).ToUpperInvariant() : OutputProfiles.VideoExtension(options.VideoContainer).ToUpperInvariant();
        string quality = options.Audio ? options.AudioBitrate > 0 ? options.AudioBitrate + " kbps" : t("Automatic") : options.Height > 0 ? options.Height + "p" : t("Best available");
        OutputDescription = format + " · " + quality;
        long? size = Entry.Transfer?.TotalBytes;
        if (IsComplete && File.Exists(OutputFile))
            try { size = new FileInfo(OutputFile).Length; } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        TransferSummary = Entry.Transfer?.DownloadedBytes is { } received && size is { } total && total > 0
            ? FileSize(received) + " / " + FileSize(total) : size.HasValue ? FileSize(size.Value) : t("Size unavailable");
        RowDescription = OutputDescription + (size is > 0 ? " · " + FileSize(size.Value) : "");
        Speed = IsActive && Entry.Transfer?.BytesPerSecond is > 0 ? FileSize((long)Entry.Transfer.BytesPerSecond.Value) + "/s" : "—";
        Remaining = IsActive && Entry.Transfer?.RemainingSeconds is > 0 and < 8640000
            ? TimeSpan.FromSeconds(Entry.Transfer.RemainingSeconds.Value).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture) : "—";
        MediaStatus = MissingFile ? t("File moved or deleted") : Status;
        PrimaryAction = MissingFile ? t("File unavailable") : IsComplete ? t("Open file") : IsActive ? t("Pause download") : Entry.State == QueueState.Pending ? t("Start download") : RetryAction;
        CaptionSummary = options.Captions == null ? t("No subtitles selected") : options.Captions.Language.ToUpperInvariant() + " · " + options.Captions.Format.ToString().ToUpperInvariant();
    }
    internal static string FileSize(long bytes) => bytes >= 1024L * 1024 * 1024 ? (bytes / (1024d * 1024 * 1024)).ToString("0.0", CultureInfo.CurrentCulture) + " GB" : bytes >= 1024 * 1024 ? (bytes / (1024d * 1024)).ToString("0.0", CultureInfo.CurrentCulture) + " MB" : (bytes / 1024d).ToString("0.0", CultureInfo.CurrentCulture) + " KB";
}
