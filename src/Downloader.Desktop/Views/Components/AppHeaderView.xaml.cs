using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

public partial class AppHeaderView : UserControl
{
    private readonly ShineMotion shine;
    private bool logoPlaying;
    private long logoGeneration;

    public AppHeaderView()
    {
        InitializeComponent();
        var light = ((LinearGradientBrush)LogoLight.Fill).Clone();
        LogoLight.Fill = light;
        shine = new ShineMotion(LogoLight, (TranslateTransform)light.RelativeTransform, durationMilliseconds: 420, peakOpacity: .32, travelDistance: .72);
        Loaded += (_, _) => { MotionPreferences.Changed += ResetLogo; };
        Unloaded += (_, _) => { MotionPreferences.Changed -= ResetLogo; ResetLogo(); };
        IsVisibleChanged += (_, _) => { if (!IsVisible) ResetLogo(); };
        BrandButton.IsVisibleChanged += (_, _) => { if (!BrandButton.IsVisible) ResetLogo(); };
        BrandButton.IsEnabledChanged += (_, _) => { if (!BrandButton.IsEnabled) ResetLogo(); };
    }

    private void LogoClicked(object sender, RoutedEventArgs args) => PlayLogo();
    private void LogoHovered(object sender, MouseEventArgs args) => PlayLogo();
    private void LogoLeft(object sender, MouseEventArgs args)
    {
        if (!LogoScale.HasAnimatedProperties) shine.FadeOut(ResetLogo);
    }

    private void PlayLogo()
    {
        if (logoPlaying || !IsLoaded || !BrandButton.IsVisible || !BrandButton.IsEnabled || !MotionPreferences.Enabled) return;
        ResetLogo(); logoPlaying = true;
        long revision = logoGeneration;
        var turn = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        turn.KeyFrames.Add(new LinearDoubleKeyFrame(1, TimeSpan.Zero));
        turn.KeyFrames.Add(new SplineDoubleKeyFrame(0, TimeSpan.FromMilliseconds(125), new KeySpline(.42, 0, 1, 1)));
        turn.KeyFrames.Add(new SplineDoubleKeyFrame(-1, TimeSpan.FromMilliseconds(260), new KeySpline(0, 0, .58, 1)));
        turn.KeyFrames.Add(new SplineDoubleKeyFrame(0, TimeSpan.FromMilliseconds(385), new KeySpline(.42, 0, 1, 1)));
        turn.KeyFrames.Add(new SplineDoubleKeyFrame(1, TimeSpan.FromMilliseconds(540), new KeySpline(.16, 1, .3, 1)));
        turn.Completed += (_, _) =>
        {
            if (logoGeneration != revision) return;
            SettleMark();
            shine.Play(() => { if (logoGeneration == revision) ResetLogo(); });
        };
        var tilt = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        foreach (var (milliseconds, angle) in new[] { (0, 0d), (125, -4d), (260, 0d), (385, 4d), (540, 0d) })
            tilt.KeyFrames.Add(new SplineDoubleKeyFrame(angle, TimeSpan.FromMilliseconds(milliseconds), new KeySpline(.37, 0, .63, 1)));
        var shade = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        shade.KeyFrames.Add(new LinearDoubleKeyFrame(1, TimeSpan.Zero));
        shade.KeyFrames.Add(new SplineDoubleKeyFrame(.84, TimeSpan.FromMilliseconds(260), new KeySpline(.4, 0, .2, 1)));
        shade.KeyFrames.Add(new SplineDoubleKeyFrame(1, TimeSpan.FromMilliseconds(540), new KeySpline(.4, 0, .2, 1)));
        LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, turn);
        LogoTilt.BeginAnimation(SkewTransform.AngleXProperty, tilt);
        BrandMark.BeginAnimation(UIElement.OpacityProperty, shade);
    }

    private void ResetLogo()
    {
        logoGeneration++; logoPlaying = false;
        shine.Stop();
        SettleMark();
    }

    private void SettleMark()
    {
        LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        LogoTilt.BeginAnimation(SkewTransform.AngleXProperty, null);
        BrandMark.BeginAnimation(UIElement.OpacityProperty, null);
    }

}
