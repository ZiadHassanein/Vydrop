using YouTubeDownloader;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 3 && args[0] == "--update-release")
            {
                var release = AppUpdates.Parse(File.ReadAllText(args[1]), Version.Parse(args[2])) ?? throw new Exception("No newer stable update is available.");
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { result = "PASS", version = release.Version.ToString(), package = release.PackageName, url = release.PackageUrl, sha256 = release.PackageHash }));
                return 0;
            }
            if (args.Length == 3 && args[0] == "--social-video-live") return await SocialVideoLiveChecks.Run(args[1], args[2]);
            if (args.Length == 2 && args[0] == "--bundle-updates") { BundledUpdateChecks.Run(args[1]); return 0; }
            if (args.Length == 1 && args[0] == "--playlist-folders") { PlaylistFolderChecks.Run(); return 0; }
            if (args.Length == 1 && args[0] == "--twitch-clips") { TwitchClipChecks.Run(); return 0; }
            if (args.Length == 2 && args[0] == "--twitch-clips-live") return await TwitchClipChecks.Live(args[1]);
            if (args.Length == 2 && args[0] == "--inspection-speed-live") return await InspectionSpeedChecks.Live(args[1]);
            if (args.Length == 2 && args[0] == "--inspection-speed") { await InspectionSpeedChecks.Run(args[1]); return 0; }
            if (args.Length == 1 && args[0] == "--browser-fresh-pc") { await BrowserFreshPcChecks.Run(); return 0; }
            if (args.Length == 1 && args[0] == "--browser-stores") { BrowserStoreChecks.Run(); BrowserRegistrationChecks.Run(); return 0; }
            if (args.Length == 1 && args[0] == "--queue-concurrency") { await QueueConcurrencyChecks.Run(); return 0; }
            if (args.Length == 1 && args[0] == "--queue-resume-progress") { await QueueResumeProgressChecks.Run(); return 0; }
            if (args.Length == 1 && args[0] == "--queue-recovery") { await QueueRecoveryChecks.Run(); return 0; }
            if (args.Length == 1 && args[0] == "--download-improvements") { BrowserLinkTransport.PipeNameOverride = "Vydrop.Improvements." + Guid.NewGuid().ToString("N"); await DownloadImprovementChecks.Run(); return 0; }
            if (args.Length == 2 && args[0] == "--native-host") return await NativeHostChecks.Run(args[1]);
            if (args.Length == 2 && args[0] == "--package") return PackageArtifactChecks.Run(args[1]);
            if (args.Length == 3 && args[0] == "--direct-media-live") return DirectMediaLiveChecks.Run(args[1], args[2]);
            if (args.Length == 4 && args[0] == "--linkedin-live") return LinkedInLiveChecks.Run(args[1], args[2], args[3]);
            if (args.Length == 4 && args[0] == "--browser-media-live") return LinkedInLiveChecks.Run(args[1], args[2], args[3], true);
            BrowserLinkTransport.PipeNameOverride = "Vydrop.Tests." + Guid.NewGuid().ToString("N");
            var output = AppContext.BaseDirectory;
            foreach (string file in Directory.GetFiles(args[0]))
                File.Copy(file, Path.Combine(output, Path.GetFileName(file)), true);
            if (CoreTests.Run() != 0)
                return 1;
            await ReliabilityChecks.Run(args[0]);
            await BoundaryChecks.Run(args[0]);
            BundledUpdateChecks.Run(args[0]);
            PlaylistFolderChecks.Run();
            await ServiceChecks.Run();
            await QueueConcurrencyChecks.Run();
            await QueueResumeProgressChecks.Run();
            await QueueRecoveryChecks.Run();
            await DownloadImprovementChecks.Run();
            await WorkflowChecks.Run(args[0]);
            OutputProfileChecks.Run(args[0]);
            TwitchClipChecks.Run();
            RedditMediaChecks.Run();
            await AutomaticToolChecks.Run(args[0]);
            await ReviewRegressionChecks.Run(args[0]);
            await InspectionSpeedChecks.Run(args[0]);
            await BrowserFreshPcChecks.Run();
            BrowserRegistrationChecks.Run();
            BrowserStoreChecks.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}
