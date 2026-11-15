using CommunityToolkit.Mvvm.ComponentModel;

namespace YouTubeDownloader;

public sealed class PreferenceChoice(int key, string text, Func<string, string> translate) : ObservableObject
{
    public int Key { get; } = key;
    public string Value => translate(text);
    internal void RefreshLanguage() => OnPropertyChanged(nameof(Value));
    public override string ToString() => Value;
}
