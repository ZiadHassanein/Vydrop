using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

internal sealed class ShineMotion(FrameworkElement layer, TranslateTransform offset)
{
    private long generation;

    internal void Play()
    {
        Stop();
        if (!MotionPreferences.Enabled) return;
        offset.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-1.1, 1.1, TimeSpan.FromMilliseconds(2000))
            { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }, FillBehavior = FillBehavior.Stop });
        var light = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        light.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.Zero));
        light.KeyFrames.Add(new SplineDoubleKeyFrame(.95, TimeSpan.FromMilliseconds(350), new KeySpline(.4, 0, .2, 1)));
        light.KeyFrames.Add(new LinearDoubleKeyFrame(.95, TimeSpan.FromMilliseconds(1550)));
        light.KeyFrames.Add(new SplineDoubleKeyFrame(0, TimeSpan.FromMilliseconds(2000), new KeySpline(.4, 0, .2, 1)));
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
