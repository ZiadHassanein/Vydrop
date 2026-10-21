using System.Text.Json;
using YouTubeDownloader;

internal static class BrowserFormatChecks
{
    internal static async Task CheckLegacySelectionAsync()
    {
        string previous = BrowserLinkTransport.PipeNameOverride;
        BrowserLinkTransport.PipeNameOverride = "Vydrop.FormatCompatibility." + Guid.NewGuid().ToString("N");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            var server = Task.Run(async () =>
            {
                for (int index = 0; index < 2; index++)
                {
                    using var pipe = new System.IO.Pipes.NamedPipeServerStream(BrowserLinkTransport.PipeName, System.IO.Pipes.PipeDirection.InOut, 1, System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
                    await pipe.WaitForConnectionAsync(deadline.Token);
                    using var message = await BrowserProtocol.ReadAsync(pipe, deadline.Token);
                    bool output = message.RootElement.TryGetProperty("output", out _);
                    if (output != (index == 0)) throw new Exception("The legacy selection fallback lost the expected envelope.");
                    await BrowserProtocol.WriteAsync(pipe, new { ok = index == 1, code = index == 0 ? "invalid" : "opened", protocol = 2 }, deadline.Token);
                }
            }, deadline.Token);
            var result = await BrowserHost.DeliverRequestAsync(new(2, "open-link", "https://example.com/video.mp4", "legacy_choice", Output: new("video", "mp4", 720)), () => { }, deadline.Token);
            await server;
            if (!result.Accepted || result.Code != "opened-legacy") throw new Exception("An older v2 desktop lost basic link delivery.");
            Console.WriteLine("PASS: An older v2 desktop receives a compatible link and an explicit selection-not-applied receipt");
        }
        finally { BrowserLinkTransport.PipeNameOverride = previous; }
    }
    internal static void Run()
    {
        static void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS: " + label); }
        using var inspect = JsonDocument.Parse("""{"version":2,"type":"inspect","requestId":"inspect_1","url":"https://example.com/video.mp4"}""");
        Check(BrowserProtocol.ReadRequest(inspect.RootElement).Type == "inspect", "Explicit format inspection accepts only the bounded browser protocol");
        using var selection = JsonDocument.Parse("""{"version":2,"type":"open-link","requestId":"pick_1","url":"https://example.com/video.mp4","output":{"kind":"video","format":"webm","height":720}}""");
        Check(BrowserProtocol.ReadRequest(selection.RootElement).Output == new BrowserOutputChoice("video", "webm", 720), "Browser output profiles retain format and height");
        foreach (string output in new[] {
            """{"kind":"video","format":"exe","height":720}""", """{"kind":"audio","format":"mp3","height":720}""",
            """{"kind":"video","format":"mp4","height":99999}""", """{"kind":"video","format":"mp4","height":720,"extra":"x"}""",
            """{"kind":"video","format":"mp4","height":720,"height":480}""", """{"kind":"video","format":"mp4","height":"720"}"""
        })
        {
            using var bad = JsonDocument.Parse("""{"version":2,"type":"open-link","requestId":"pick_2","url":"https://example.com/video.mp4","output":OUTPUT}""".Replace("OUTPUT", output, StringComparison.Ordinal));
            bool rejected = false; try { BrowserProtocol.ReadRequest(bad.RootElement); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Untrusted output choices cannot introduce unsupported formats, extra arguments or invalid dimensions");
        }
        var media = new VideoInfo { Title = new string('x', 900), HasAudio = true, Qualities = new() { new() { Resolution = 720, SelectionKey = "private-selector" }, new() { Resolution = 720 }, new() { Resolution = -1 } } };
        string json = JsonSerializer.Serialize(BrowserMediaFormats.From(media));
        using var formats = JsonDocument.Parse(json);
        Check(formats.RootElement.GetProperty("title").GetString().Length == 600 && formats.RootElement.GetProperty("videos")[0].GetProperty("heights").GetArrayLength() == 1 && !json.Contains("private-selector", StringComparison.Ordinal), "Inspection responses bound metadata and never expose internal extractor selectors");
    }
}
