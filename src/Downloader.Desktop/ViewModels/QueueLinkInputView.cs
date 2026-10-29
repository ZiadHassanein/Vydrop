using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace YouTubeDownloader;

public sealed record QueueMediaTypeChoice(string Label, bool Audio)
{
    public override string ToString() => Label;
}

public sealed class QueueLinkInputView : ObservableObject
{
    private static readonly string[] VideoFormats = { "MP4", "MKV", "WebM" };
    private static readonly string[] AudioFormats = { "MP3", "M4A", "Opus", "WAV", "FLAC" };
    private static readonly int[] HeightLimits = { 0, 4320, 2160, 1440, 1080, 720, 480, 360, 240, 144 };
    private readonly Func<string, string> translate;
    private readonly Action<QueueLinkInputView> changed;
    private string url = "", label = "", error = "";
    private bool audio, checking, playlist;
    private VideoContainer videoFormat;
    private AudioEncoding audioFormat;
    private int height, bitrate;
    private VideoInfo metadata;
    private CancellationTokenSource inspection;
    private QualityChoice selectedQuality;

    internal QueueLinkInputView(Func<string, string> translate, Action<QueueLinkInputView> changed, DownloadOptions defaults)
    {
        (this.translate, this.changed) = (translate, changed);
        audio = defaults.Audio; videoFormat = defaults.VideoContainer; audioFormat = defaults.AudioEncoding;
        height = HeightLimits.Contains(defaults.Height) ? defaults.Height : 1080;
        RefreshQualities();
    }

    public string Label => label;
    public string Error => translate(error);
    public bool HasError => error.Length > 0;
    public bool IsChecking => checking;
    public bool HasOutputOptions => metadata != null || playlist;
    public bool HasPendingOutput => !string.IsNullOrWhiteSpace(Url) && !HasOutputOptions && !HasError;
    public bool CanChooseOutput => HasOutputOptions && !checking;
    internal bool NeedsInspection => HasPendingOutput && !checking;
    internal DateTime ChangedAtUtc { get; private set; }
    public string StatusLabel => translate(checking ? "Checking formats…" : "Waiting to check formats…");
    public string TypeLabel => translate("Type");
    public string FormatLabel => translate("Format");
    public string QualityLabel => translate("Quality");
    public string LinkHint => translate("Video or playlist link");
    public string TypeAccessibleName => label + " · " + TypeLabel;
    public string FormatAccessibleName => label + " · " + FormatLabel;
    public string QualityAccessibleName => label + " · " + QualityLabel;
    public string OutputHint => playlist ? translate("Quality is checked for each playlist video.") : "";
    public bool HasOutputHint => OutputHint.Length > 0;
    public IReadOnlyList<QueueMediaTypeChoice> MediaTypes => new[]
    {
        new QueueMediaTypeChoice(translate("Video"), false), new QueueMediaTypeChoice(translate("Audio"), true)
    }.Where(choice => playlist || metadata != null && (choice.Audio ? metadata.HasAudio : Enum.GetValues<VideoContainer>().Any(container => metadata.QualitiesFor(container).Count > 0))).ToArray();
    public IReadOnlyList<string> Formats => !HasOutputOptions ? Array.Empty<string>() : audio ? AudioFormats : VideoFormats.Where(format =>
        playlist ? format.Equals(videoFormat.ToString(), StringComparison.OrdinalIgnoreCase) : metadata.QualitiesFor(Enum.Parse<VideoContainer>(format, true)).Count > 0).ToArray();
    public ObservableCollection<QualityChoice> Qualities { get; } = new();
    public bool CanChooseQuality => !checking && Qualities.Count > 1;
    public bool Audio
    {
        get => audio;
        set
        {
            if (value == audio || !MediaTypes.Any(choice => choice.Audio == value)) return;
            audio = value; RefreshQualities(); OnPropertyChanged(string.Empty);
        }
    }
    public string SelectedFormat
    {
        get => audio ? AudioFormats.First(format => format.Equals(audioFormat.ToString(), StringComparison.OrdinalIgnoreCase)) :
            VideoFormats.First(format => format.Equals(videoFormat.ToString(), StringComparison.OrdinalIgnoreCase));
        set
        {
            if (value == null || !Formats.Contains(value) || value == SelectedFormat) return;
            if (audio) audioFormat = Enum.Parse<AudioEncoding>(value, true);
            else videoFormat = Enum.Parse<VideoContainer>(value, true);
            RefreshQualities(); OnPropertyChanged(string.Empty);
        }
    }
    public QualityChoice SelectedQuality
    {
        get => selectedQuality;
        set
        {
            if (value == null || !Qualities.Contains(value) || !SetProperty(ref selectedQuality, value)) return;
            if (audio) bitrate = value.Bitrate; else height = value.Height;
            OnPropertyChanged(nameof(SelectedQualityKey));
        }
    }
    public string SelectedQualityKey
    {
        get => selectedQuality?.Key;
        set { if (value != null && Qualities.FirstOrDefault(choice => choice.Key == value) is { } quality) SelectedQuality = quality; }
    }
    public string Url
    {
        get => url;
        set
        {
            if (!SetProperty(ref url, value ?? "")) return;
            CancelInspection(); metadata = null; playlist = false; bitrate = 0; ChangedAtUtc = DateTime.UtcNow;
            RefreshQualities(); SetError(""); OnPropertyChanged(string.Empty); changed(this);
        }
    }

    private void RefreshQualities()
    {
        if (!HasOutputOptions)
        {
            Qualities.Clear(); selectedQuality = null;
            OnPropertyChanged(nameof(Qualities)); OnPropertyChanged(nameof(SelectedQualityKey)); OnPropertyChanged(nameof(CanChooseQuality));
            return;
        }
        IEnumerable<QualityChoice> choices;
        if (audio)
            choices = new[] { new QualityChoice(translate(OutputProfiles.IsLossless(audioFormat) ? "No bitrate setting" : "Automatic"), Key: "a0") }
                .Concat(OutputProfiles.AudioBitrates(audioFormat, metadata?.AudioSource).Reverse()
                    .Select(rate => new QualityChoice(rate + " kbps", Key: "a" + rate, Bitrate: rate))).ToArray();
        else if (metadata != null)
            choices = metadata.QualitiesFor(videoFormat).OrderByDescending(quality => quality.Resolution)
                .Select(quality => new QualityChoice(quality.Resolution + "p", Key: "v" + quality.Resolution, Height: quality.Resolution)).ToArray();
        else
            choices = new[] { new QualityChoice(translate("Best available"), Key: "v0") };
        var available = choices.ToArray();
        Qualities.Clear();
        foreach (var choice in available) Qualities.Add(choice);
        selectedQuality = audio ? Qualities.FirstOrDefault(choice => choice.Bitrate == bitrate) ?? Qualities[0] :
            Qualities.FirstOrDefault(choice => choice.Height == height) ?? Qualities.FirstOrDefault(choice => height == 0 || choice.Height > 0 && choice.Height <= height) ?? (Qualities.Count == 0 ? null : Qualities[^1]);
        if (audio) bitrate = selectedQuality.Bitrate;
        else if (selectedQuality != null) height = selectedQuality.Height;
        OnPropertyChanged(nameof(Qualities)); OnPropertyChanged(nameof(SelectedQuality)); OnPropertyChanged(nameof(SelectedQualityKey)); OnPropertyChanged(nameof(CanChooseQuality));
    }

    internal CancellationTokenSource BeginInspection()
    {
        CancelInspection(); inspection = new CancellationTokenSource(); checking = true; SetError("");
        OnPropertyChanged(string.Empty); changed(this); return inspection;
    }
    internal bool IsCurrentInspection(CancellationTokenSource check, string candidate) =>
        ReferenceEquals(inspection, check) && !check.IsCancellationRequested && candidate == Url.Trim();
    internal void EndInspection(CancellationTokenSource check)
    {
        if (ReferenceEquals(inspection, check)) { inspection = null; checking = false; OnPropertyChanged(string.Empty); changed(this); }
        check.Dispose();
    }
    internal void CancelInspection()
    {
        inspection?.Cancel(); inspection = null; checking = false;
        OnPropertyChanged(string.Empty);
    }
    internal void SetMetadata(VideoInfo info)
    {
        if (info.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
        if (info.Collection == null && !info.HasAudio && !Enum.GetValues<VideoContainer>().Any(container => info.QualitiesFor(container).Count > 0))
            throw new InvalidOperationException("The site did not return downloadable formats for this video.");
        metadata = info.Collection == null ? info : null; playlist = info.Collection != null;
        if (!MediaTypes.Any(choice => choice.Audio == audio)) audio = MediaTypes[0].Audio;
        if (!Formats.Contains(SelectedFormat)) videoFormat = Enum.Parse<VideoContainer>(Formats[0], true);
        RefreshQualities(); OnPropertyChanged(string.Empty);
    }
    internal void UsePlaylistOptions() { metadata = null; playlist = true; videoFormat = VideoContainer.Mp4; height = 0; RefreshQualities(); OnPropertyChanged(string.Empty); }
    internal void ResetInspection()
    {
        CancelInspection(); metadata = null; playlist = false; bitrate = 0; ChangedAtUtc = DateTime.UtcNow;
        SetError(""); RefreshQualities(); OnPropertyChanged(string.Empty); changed(this);
    }
    internal DownloadOptions OutputPreferences => new() { Audio = audio, VideoContainer = videoFormat, AudioEncoding = audioFormat, Height = height, AudioBitrate = bitrate };
    internal string InspectedTitle => metadata?.Title;
    internal void ApplyOutput(DownloadOptions request)
    {
        request.Audio = audio; request.VideoContainer = videoFormat; request.AudioEncoding = audioFormat;
        request.Height = height; request.AudioBitrate = bitrate; request.AudioSource = metadata?.AudioSource;
        request.Selection = null; request.QualityKey = null; request.Playlist = null;
        // Never attach a different editor video's identity or stale selection to a batch link.
        request.StableOutputName = metadata?.Title; request.ThumbnailUrl = metadata?.ThumbnailUrl;
        request.Uploader = metadata?.Uploader; request.Duration = metadata?.Duration ?? 0;
        request.SourcePage = null; request.MediaId = null;
    }
    internal void SetError(string message)
    {
        error = message; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError));
    }
    internal void RefreshLabel(string value)
    {
        label = value; RefreshQualities(); OnPropertyChanged(string.Empty);
    }
}
