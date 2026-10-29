namespace YouTubeDownloader;

internal static class AudioBitrateConversion
{
    internal static string[] MetadataArguments(DownloadOptions options)
    {
        // ExtractAudio may keep a matching codec without lowering its bitrate.
        // Convert during Metadata, before EmbedThumbnail, so cover art survives.
        if (!options.Audio || options.AudioBitrate == 0 || OutputProfiles.IsLossless(options.AudioEncoding) || options.AudioSource?.Matches(options.AudioEncoding) != true)
            return Array.Empty<string>();
        int bitrate = OutputProfiles.EffectiveAudioBitrate(options);
        string encoder = options.AudioEncoding switch { AudioEncoding.Mp3 => "libmp3lame", AudioEncoding.M4a => "aac", _ => "libopus" };
        return new[] { "--postprocessor-args", "Metadata+ffmpeg_o:-c:a " + encoder + " -b:a " + bitrate + "k" };
    }
}
