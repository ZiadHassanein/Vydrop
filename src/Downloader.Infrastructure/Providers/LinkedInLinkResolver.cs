using System.Text.RegularExpressions;

namespace YouTubeDownloader;

public sealed class LinkedInLinkResolver : IMediaLinkResolver
{
    public bool Supports(Uri uri) => uri.Host is "linkedin.com" or "www.linkedin.com" or "m.linkedin.com";

    public MediaTargets Resolve(string input)
    {
        string valid = MediaLink.PublicUrl(input);
        var uri = new Uri(valid);
        if (!Supports(uri)) return new(valid, null);
        string path = Uri.UnescapeDataString(uri.AbsolutePath);
        var activity = Regex.Match(path, @"^/feed/update/urn:li:activity:(\d{8,25})/?$");
        if (activity.Success) return Activity(activity.Groups[1].Value);
        if (Regex.IsMatch(path, @"^/feed/update/urn:li:groupPost:\d+-\d+/?$"))
        {
            // Group IDs and activity IDs differ. Only use an activity explicitly supplied by LinkedIn's share link.
            var highlighted = uri.Query.TrimStart('?').Split('&')
                .Select(part => part.Split('=', 2))
                .Where(pair => pair.Length == 2 && Uri.UnescapeDataString(pair[0]) == "highlightedUpdateUrn")
                .Select(pair => Uri.UnescapeDataString(pair[1])).ToArray();
            if (highlighted.Length == 1)
            {
                activity = Regex.Match(highlighted[0], @"^urn:li:activity:(\d{8,25})$");
                if (activity.Success) return Activity(activity.Groups[1].Value);
            }
        }
        return new(valid, null);
    }

    private static MediaTargets Activity(string id) => new($"https://www.linkedin.com/feed/update/urn:li:activity:{id}/", null);
}
