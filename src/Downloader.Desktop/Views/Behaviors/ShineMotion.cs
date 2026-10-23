using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

internal sealed class ShineMotion(FrameworkElement layer, TranslateTransform offset, double durationMilliseconds = 2000, double peakOpacity = .95, double travelDistance = 1.1)
{
    private long generation;

    internal void Play(Action completed = null)
    {
        Stop();
        if (!MotionPreferences.Enabled) return;
        long revision = generation;
        offset.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-travelDistance, travelDistance, TimeSpan.FromMilliseconds(durationMilliseconds))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }, FillBehavior = FillBehavior.Stop });
        var light = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        light.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.Zero));
        light.KeyFrames.Add(new SplineDoubleKeyFrame(peakOpacity, TimeSpan.FromMilliseconds(durationMilliseconds * .175), new KeySpline(.4, 0, .2, 1)));
        light.KeyFrames.Add(new LinearDoubleKeyFrame(peakOpacity, TimeSpan.FromMilliseconds(durationMilliseconds * .775)));
        light.KeyFrames.Add(new SplineDoubleKeyFrame(0, TimeSpan.FromMilliseconds(durationMilliseconds), new KeySpline(.4, 0, .2, 1)));
        light.Completed += (_, _) => { if (generation == revision) { Stop(); completed?.Invoke(); } };
        layer.BeginAnimation(UIElement.OpacityProperty, light);
    }

    internal void FadeOut(Action completed = null)
    {
        if (!MotionPreferences.Enabled || layer.Opacity <= 0) { Stop(); completed?.Invoke(); return; }
        long revision = generation;
        var fade = new DoubleAnimation(layer.Opacity, 0, TimeSpan.FromMilliseconds(180))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop };
        fade.Completed += (_, _) => { if (generation == revision) { Stop(); completed?.Invoke(); } };
        layer.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    internal void Stop()
    {
        generation++;
        offset.BeginAnimation(TranslateTransform.XProperty, null);
        layer.BeginAnimation(UIElement.OpacityProperty, null);
    }
}
