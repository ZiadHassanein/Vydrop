namespace YouTubeDownloader;
public sealed class DownloadProgress
{
    public long OperationId;
    public string Stage, Detail;
    public double Percent;
    public bool Indeterminate;
    public int Item, Total;
    public string ItemTitle;
    public long? DownloadedBytes, TotalBytes;
    public double? BytesPerSecond, RemainingSeconds;
}
