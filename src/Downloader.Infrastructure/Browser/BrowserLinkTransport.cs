using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace YouTubeDownloader;
internal enum BrowserDelivery { NoListener, Rejected, Accepted }
public static class BrowserLinkTransport
{
    internal static string PipeNameOverride { get; set; }
    public static string PipeName => PipeNameOverride ?? (ReleaseChannel.IsPreview ? "ViFree.Preview." : "ViFree.") + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(WindowsIdentity.GetCurrent().User.Value)))[..24];
    internal static async Task<BrowserDelivery> TrySendAsync(string url, int milliseconds, CancellationToken token)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(milliseconds, token).ConfigureAwait(false);
            await BrowserProtocol.WriteAsync(pipe, new { version = 1, type = "open-link", url }, token).ConfigureAwait(false);
            using var reply = await BrowserProtocol.ReadAsync(pipe, token).ConfigureAwait(false);
            return reply.RootElement.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True ? BrowserDelivery.Accepted : BrowserDelivery.Rejected;
        }
        catch (OperationCanceledException) { throw; }
        catch (IOException) { return BrowserDelivery.NoListener; }
        catch (TimeoutException) { return BrowserDelivery.NoListener; }
    }
    public static async Task ListenAsync(Func<string, bool> received, CancellationToken token)
        => await ListenRequestsAsync(request => new(received(request.Url), "opened"), token).ConfigureAwait(false);
    internal static async Task<BrowserReceipt> TrySendRequestAsync(BrowserRequest request, int milliseconds, CancellationToken token)
        => (await TrySendCompatibleRequestAsync(request, milliseconds, token).ConfigureAwait(false)).Receipt;
    internal static async Task<(BrowserReceipt Receipt, bool Legacy)> TrySendCompatibleRequestAsync(BrowserRequest request, int milliseconds, CancellationToken token)
    {
        bool connected = false;
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(milliseconds, token).ConfigureAwait(false);
            connected = true;
            var envelope = new Dictionary<string, object> { ["version"] = 2, ["type"] = request.Type, ["requestId"] = request.RequestId };
            if (request.Url != null) envelope["url"] = request.Url;
            if (request.SourcePage != null) envelope["sourcePage"] = request.SourcePage;
            if (request.MediaId != null) envelope["mediaId"] = request.MediaId;
            if (request.WholePlaylist) envelope["scope"] = "playlist";
            if (request.Output != null) envelope["output"] = new { kind = request.Output.Kind, format = request.Output.Format, height = request.Output.Height };
            await BrowserProtocol.WriteAsync(pipe, envelope, token).ConfigureAwait(false);
            using var reply = await BrowserProtocol.ReadAsync(pipe, token).ConfigureAwait(false);
            // Older apps reject v2 with an unversioned reply. Let the host retry the exact v1 envelope.
            if (!reply.RootElement.TryGetProperty("protocol", out var protocol) || !protocol.TryGetInt32(out int version) || version != 2) return (null, true);
            return (new(reply.RootElement.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True,
                reply.RootElement.TryGetProperty("code", out var code) ? code.GetString() : "busy"), false);
        }
        catch (OperationCanceledException) { throw; }
        // Old listeners can close an unsupported v2 frame without a reply. Only
        // allow legacy delivery after reaching a listener, never on startup timeout.
        catch (Exception ex) when (ex is IOException or TimeoutException or JsonException) { return (null, connected); }
    }
    public static async Task ListenRequestsAsync(Func<BrowserRequest, BrowserReceipt> received, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(TimeSpan.FromSeconds(3));
                using var message = await BrowserProtocol.ReadAsync(pipe, deadline.Token).ConfigureAwait(false);
                var request = BrowserProtocol.ReadRequest(message.RootElement);
                var receipt = received(request);
                await BrowserProtocol.WriteAsync(pipe, new { ok = receipt.Accepted, code = receipt.Code, protocol = 2 }, deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { if (token.IsCancellationRequested) return; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                try { await Task.Delay(1000, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
            catch (JsonException) { }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
        }
    }
}
