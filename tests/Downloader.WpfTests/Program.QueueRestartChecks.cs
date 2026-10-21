using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using YouTubeDownloader;

internal static partial class Program
{
    // Opt-in live check: real desktop windows, new processes and installed media tools.
    // Every queue, partial file and completed download belongs to the supplied test directory.
    private const string RestartSampleUrl = "https://www.w3schools.com/html/mov_bbb.mp4";
    private static readonly JsonSerializerOptions RestartJson = new() { IncludeFields = true, WriteIndented = true };

    private static int CheckQueueRestartProcesses(string output)
    {
        try
        {
            string root = Path.GetFullPath(output);
            Directory.CreateDirectory(root);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            byte[] original = client.GetByteArrayAsync(RestartSampleUrl).GetAwaiter().GetResult();
            Check(original.Length > 512 * 1024, "The independent public reference is large enough to interrupt before completion");
            File.WriteAllBytes(Path.Combine(root, "reference.mp4"), original);
            foreach (string scenario in new[] { "close", "terminate" })
            {
                string state = Path.Combine(root, scenario);
                // Refuse reuse: a previous run must never supply this run's readiness marker.
                if (Directory.Exists(state)) throw new IOException("Choose a new test output directory.");
                Directory.CreateDirectory(state);
                File.WriteAllText(Path.Combine(state, "reference.sha256"), Convert.ToHexString(SHA256.HashData(original)));
                RunQueueRestartChild(state, "seed", scenario);
                RunQueueRestartChild(state, "interrupt", scenario);
                RunQueueRestartChild(state, "resume", scenario);
                RunQueueRestartChild(state, "completed", scenario);
            }
            Console.WriteLine("PASS: Normal closing and process termination preserve the queue and resume exact media bytes across fresh desktop processes.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void RunQueueRestartChild(string root, string phase, string scenario)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath)
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (string value in new[] { "--queue-restart-child", root, phase, scenario }) start.ArgumentList.Add(value);
        using var child = Process.Start(start) ?? throw new IOException("The test process could not start.");
        Task<string> stdout = child.StandardOutput.ReadToEndAsync(), stderr = child.StandardError.ReadToEndAsync();
        bool killed = false;
        try
        {
            if (phase == "interrupt" && scenario == "terminate")
            {
                var deadline = Stopwatch.StartNew();
                string marker = Path.Combine(root, "ready-to-terminate.json");
                while (!File.Exists(marker) && !child.HasExited && deadline.Elapsed < TimeSpan.FromSeconds(60)) Thread.Sleep(100);
                if (!File.Exists(marker)) throw new TimeoutException("The owned download process did not reach its interruption checkpoint.");
                child.Kill(entireProcessTree: true);
                killed = true;
            }
            if (!child.WaitForExit(180000)) throw new TimeoutException("The queue restart phase did not finish: " + phase);
        }
        finally
        {
            if (!child.HasExited) { child.Kill(entireProcessTree: true); child.WaitForExit(10000); }
            string log = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
            File.WriteAllText(Path.Combine(root, phase + ".log"), log);
            Console.Write(log);
        }
        Check(killed || child.ExitCode == 0, "Fresh desktop process completed " + scenario + "/" + phase);
        var saved = new LocalQueueStorage(root).Load();
        Check(saved.Jobs.Count == 2 && saved.Jobs[1].State == (phase is "resume" or "completed" ? QueueState.Completed : QueueState.Pending),
            "Both jobs remain durable after process exit: " + scenario + "/" + phase);
        if (phase == "interrupt")
            Check(killed ? saved.Jobs[0].State is QueueState.Preparing or QueueState.Downloading : saved.Jobs[0].State == QueueState.Paused,
                "Disk records the real shutdown state before a fresh launch: " + scenario);
        File.Copy(Path.Combine(root, "queue.json"), Path.Combine(root, "expected-queue.json"), overwrite: true);
    }

    private static int CheckQueueRestartChild(string root, string phase, string scenario)
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        string toolsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links");
        var tools = ToolSet.Validated(Path.Combine(toolsRoot, "yt-dlp.exe"), Path.Combine(toolsRoot, "ffmpeg.exe"), Path.Combine(toolsRoot, "ffprobe.exe"), Path.Combine(toolsRoot, "deno.exe"));
        var service = new DownloadService(new YtDlpProvider(tools), new LocalPlaylistStorage(Path.Combine(root, "playlist-state")));
        var window = new MainWindow(service, new SettingsRepository(root), new FixtureThumbnailLoader()) { ShowActivated = false, ShowInTaskbar = false };
        var model = (MainViewModel)window.DataContext;
        Exception failure = null;
        window.ContentRendered += async (_, _) =>
        {
            try
            {
                await Task.Delay(150);
                if (phase == "seed")
                {
                    Check(model.QueueItems.Count == 0, "The live test starts with isolated empty application data");
                    foreach (string name in new[] { "Interrupted sample", "Waiting sample" })
                        model.Queue.Add(new DownloadOptions
                        {
                            Url = RestartSampleUrl, Folder = Path.Combine(root, name), StableOutputName = name,
                            Height = 176, BytesPerSecond = name == "Interrupted sample" ? 64 * 1024 : 0,
                            Connections = 1, MaximumAttempts = 1, DirectFileAcceleration = false
                        }, name);
                }
                else
                {
                    var original = JsonSerializer.Deserialize<QueueSnapshot>(File.ReadAllText(Path.Combine(root, "expected-queue.json")), RestartJson);
                    Check(model.Queue.Entries.Select(e => e.Id).SequenceEqual(original.Jobs.Select(e => e.Id)), "Reopening preserves queue identities and order");
                    Check(model.Queue.Entries.Zip(original.Jobs).All(pair => pair.First.Url == pair.Second.Options.Url && pair.First.Title == pair.Second.Title &&
                        pair.First.Options.Folder == pair.Second.Options.Folder && pair.First.Options.Height == pair.Second.Options.Height &&
                        pair.First.Options.BytesPerSecond == pair.Second.Options.BytesPerSecond && pair.First.Options.Connections == pair.Second.Options.Connections),
                        "Reopening preserves titles, links, output folders, quality and download limits");
                    Check(!model.QueueRunning && !model.IsBusy, "Reopening does not silently start pending or interrupted downloads");
                }
                if (phase == "interrupt")
                {
                    var running = model.QueueStartCommand.ExecuteAsync(null);
                    var deadline = Stopwatch.StartNew();
                    while (PartialBytes(root) < 256 * 1024 && !running.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(55)) await Task.Delay(50);
                    Check(!running.IsCompleted && model.Queue.Entries[0].State == QueueState.Downloading && model.Queue.Entries[1].State == QueueState.Pending && PartialBytes(root) >= 256 * 1024,
                        "A real yt-dlp download has saved partial bytes while the next queued job still waits");
                    Capture(window, Path.Combine(root, "before-interruption.png"));
                    WriteRestartReceipt(root, "before-interruption", model);
                    if (scenario == "terminate")
                    {
                        File.WriteAllText(Path.Combine(root, "ready-to-terminate.json"), "{\"ready\":true}");
                        await Task.Delay(Timeout.Infinite);
                    }
                }
                else if (phase == "resume")
                {
                    QueueState expected = scenario == "close" ? QueueState.Paused : QueueState.Interrupted;
                    Check(model.Queue.Entries[0].State == expected && model.Queue.Entries[1].State == QueueState.Pending &&
                        model.QueueItems[0].CanRetry && model.QueueItems[0].RetryAction == "Resume", "A fresh desktop exposes Resume for the recovered job and keeps the next job pending");
                    long partial = PartialBytes(root);
                    Check(partial > 0, "Partial media files survive closing or terminating the app");
                    await Task.Delay(500);
                    Check(PartialBytes(root) == partial && !model.QueueRunning, "Recovered downloads wait for explicit user action");
                    await InspectQueueModal(window, model, async dialog =>
                    {
                        await Task.Delay(100);
                        Capture(dialog, Path.Combine(root, "queue-restored.png"));
                    });
                    double? firstProgress = null;
                    service.Progress += progress =>
                    {
                        if (progress.Stage == "Downloading" && progress.Percent > 0) firstProgress ??= progress.Percent;
                    };
                    model.RetryQueueCommand.Execute(model.Queue.Entries[0].Id);
                    await model.QueueStartCommand.ExecuteAsync(null).WaitAsync(TimeSpan.FromSeconds(120));
                    Check(model.Queue.Entries.All(e => e.State == QueueState.Completed), "Resuming completes the interrupted job and the remaining queued job");
                    long referenceLength = new FileInfo(Path.Combine(Directory.GetParent(root).FullName, "reference.mp4")).Length;
                    Check(firstProgress.HasValue && firstProgress >= 100.0 * partial / referenceLength,
                        "The real downloader resumes beyond its saved offset instead of restarting from zero");
                    VerifyRestartOutputs(root, model);
                    Capture(window, Path.Combine(root, "downloads-complete.png"));
                    WriteRestartReceipt(root, "resumed", model, firstProgress);
                }
                else if (phase == "completed")
                {
                    Check(model.Queue.Entries.All(e => e.State == QueueState.Completed), "Completed queue states survive another complete process restart");
                    VerifyRestartOutputs(root, model);
                    Check(model.RecentDownloadCount == 2, "Both completed files remain in recent downloads without duplicate history");
                    WriteRestartReceipt(root, "completed-reopened", model);
                }
            }
            catch (Exception ex) { failure = ex; Console.Error.WriteLine(ex); }
            finally { window.Close(); }
        };
        app.Run(window);
        if (failure != null) return 1;
        if (phase == "interrupt" && scenario == "close")
        {
            Check(new LocalQueueStorage(root).Load().Jobs[0].State == QueueState.Paused, "Closing the actual main window waits for cancellation and persists the paused job");
            Check(PartialBytes(root) > 0, "Normal app shutdown retains resumable media parts");
        }
        Console.WriteLine("PASS: Desktop process " + Environment.ProcessId + " finished " + scenario + "/" + phase);
        return 0;
    }

    private static long PartialBytes(string root) => Directory.EnumerateFiles(root, "*.part", SearchOption.AllDirectories).Sum(path => new FileInfo(path).Length);

    private static void VerifyRestartOutputs(string root, MainViewModel model)
    {
        string expected = File.ReadAllText(Path.Combine(root, "reference.sha256"));
        var files = model.Queue.Entries.Select(e => e.Result?.FilePath).ToArray();
        Check(files.All(File.Exists) && files.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 2, "Each queued job produces one distinct completed output");
        Check(files.All(file => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) == expected), "Both resumed outputs match the independently downloaded original by SHA-256");
        Check(Directory.EnumerateFiles(root, "*.mp4", SearchOption.AllDirectories).Count() == 2 && PartialBytes(root) == 0, "Completion leaves exactly two media files and no unfinished parts");
    }

    private static void WriteRestartReceipt(string root, string name, MainViewModel model, double? firstProgress = null) =>
        File.WriteAllText(Path.Combine(root, name + ".json"), JsonSerializer.Serialize(new
        {
            processId = Environment.ProcessId, partialBytes = PartialBytes(root), firstResumedPercent = firstProgress,
            jobs = model.Queue.Entries.Select(entry => new { entry.Id, entry.Title, state = entry.State.ToString(), entry.Percent, file = entry.Result?.FilePath })
        }, RestartJson));
}
