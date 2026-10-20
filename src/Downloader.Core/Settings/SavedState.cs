namespace YouTubeDownloader;
public sealed class SavedState
{
    public string Folder;
    public int FolderVersion;
    public string Theme = "Dark";
    public bool ThemeSelected;
    public string Language = "en";
    public int Format, Quality;
    public int AudioBitrate;
    public AudioEncoding AudioEncoding = AudioEncoding.Mp3;
    public VideoContainer VideoContainer;
    public int UnseenDownloads;
    public int Connections = 1;
    public int SpeedLimitKiB;
    public int QueueAttempts = 2;
    public bool DirectFileAcceleration;
    public List<string> Recent = new List<string>();
}
