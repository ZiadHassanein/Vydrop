using System.Buffers.Binary;
using System.Text.Json;

namespace YouTubeDownloader;
public sealed record BrowserRequest(int Version, string Type, string Url = null, string RequestId = null, string SourcePage = null, string MediaId = null);
public sealed record BrowserReceipt(bool Accepted, string Code, string Message = null);
public static class BrowserProtocol
{
    public const int MaximumBytes = 65536;
    public static async Task<JsonDocument> ReadAsync(Stream input, CancellationToken token)
    {
        var header = new byte[4];
        await input.ReadExactlyAsync(header, token).ConfigureAwait(false);
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (count == 0 || count > MaximumBytes) throw new InvalidDataException("The browser message is too large or empty.");
        var body = new byte[count];
        await input.ReadExactlyAsync(body, token).ConfigureAwait(false);
        return JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 8 });
    }
    public static async Task WriteAsync(Stream output, object value, CancellationToken token)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(value);
        if (body.Length > MaximumBytes) throw new InvalidDataException("The browser response is too large.");
        var header = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)body.Length);
        await output.WriteAsync(header, token).ConfigureAwait(false);
        await output.WriteAsync(body, token).ConfigureAwait(false);
        await output.FlushAsync(token).ConfigureAwait(false);
    }
    public static string ReadUrl(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object || message.EnumerateObject().Count() != 3 ||
            !message.TryGetProperty("version", out var version) || !version.TryGetInt32(out int number) || number != 1 ||
            !message.TryGetProperty("type", out var type) || type.GetString() != "open-link" ||
            !message.TryGetProperty("url", out var url) || url.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("The browser message is not supported.");
        return MediaLink.PublicUrl(url.GetString());
    }
    public static BrowserRequest ReadRequest(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object || message.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != message.EnumerateObject().Count()) throw new InvalidDataException("The browser message is invalid.");
        if (!message.TryGetProperty("version", out var version) || !version.TryGetInt32(out int number)) throw new InvalidDataException("The browser message is not supported.");
        if (number == 1) return new(1, "open-link", ReadUrl(message));
        if (number != 2 || message.EnumerateObject().Any(p => p.Name is not ("version" or "type" or "url" or "requestId" or "sourcePage" or "mediaId"))) throw new InvalidDataException("The browser message is not supported.");
        if (message.EnumerateObject().Any(p => p.Name != "version" && p.Value.ValueKind != JsonValueKind.String)) throw new InvalidDataException("The browser fields must be strings.");
        static string Text(JsonElement root, string key) => root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        string type = Text(message, "type"), id = Text(message, "requestId"), mediaId = Text(message, "mediaId");
        if (type is not ("ping" or "open-link" or "enqueue") || id == null || !System.Text.RegularExpressions.Regex.IsMatch(id, @"^[A-Za-z0-9_-]{1,64}$") ||
            mediaId != null && !System.Text.RegularExpressions.Regex.IsMatch(mediaId, @"^[A-Za-z0-9_-]{1,128}$")) throw new InvalidDataException("The browser request is invalid.");
        if (type == "ping")
        {
            if (message.EnumerateObject().Count() != 3) throw new InvalidDataException("The health request is invalid.");
            return new(2, type, RequestId: id);
        }
        string url = MediaLink.PublicUrl(Text(message, "url"));
        string page = Text(message, "sourcePage");
        if (page != null) page = MediaLink.PublicUrl(page);
        return new(2, type, url, id, page, mediaId);
    }
}
