using System.Text.Json;
namespace YouTubeDownloader;

public sealed class LocalQueueStorage : IQueueStorage
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true, MaxDepth = 24 };
    private readonly string directory;
    private readonly object gate = new();
    public string FilePath => Path.Combine(directory, "queue.json");
    public LocalQueueStorage(string directory = null) => this.directory = directory ?? AppPaths.UserData;
    public QueueSnapshot Load()
    {
        lock (gate)
        {
            if (!File.Exists(FilePath)) return new();
            try { return Read(FilePath); }
            catch (Exception ex) when (ex is JsonException or InvalidDataException)
            {
                if (!File.Exists(FilePath + ".backup")) throw;
                var backup = Read(FilePath + ".backup");
                File.Copy(FilePath, FilePath + ".damaged-" + Guid.NewGuid().ToString("N"));
                return backup;
            }
        }
    }
    private static QueueSnapshot Read(string path)
    {
        if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("The queue file is too large.");
        return JsonSerializer.Deserialize<QueueSnapshot>(File.ReadAllBytes(path), Json) ?? throw new InvalidDataException("The queue is empty.");
    }
    public void Save(QueueSnapshot snapshot)
    {
        lock (gate)
        {
            Directory.CreateDirectory(directory);
            string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, Json);
                if (bytes.Length > 4 * 1024 * 1024) throw new InvalidDataException("The queue is too large to save.");
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(bytes); stream.Flush(true); }
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".backup");
                else File.Move(temporary, FilePath);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
