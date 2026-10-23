using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

public partial class AppHeaderView : UserControl
{
    private bool logoPlaying;
    private long logoGeneration;

    public AppHeaderView()
    {
        InitializeComponent();
        Loaded += (_, _) => { MotionPreferences.Changed += ResetLogo; };
        Unloaded += (_, _) => { MotionPreferences.Changed -= ResetLogo; ResetLogo(); };
        BrandButton.IsVisibleChanged += (_, _) => { if (!BrandButton.IsVisible) ResetLogo(); };
        BrandButton.IsEnabledChanged += (_, _) => { if (!BrandButton.IsEnabled) ResetLogo(); };
    }

    private void LogoClicked(object sender, RoutedEventArgs e) => PlayLogo();
    private void LogoHovered(object sender, MouseEventArgs e) => PlayLogo();

    private void PlayLogo()
    {
        if (logoPlaying || !IsLoaded || !BrandButton.IsVisible || !BrandButton.IsEnabled || !MotionPreferences.Enabled) return;
        ResetLogo(); logoPlaying = true;
        long revision = logoGeneration;
        var slide = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        slide.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.Zero));
        slide.KeyFrames.Add(new SplineDoubleKeyFrame(52, TimeSpan.FromMilliseconds(200), new KeySpline(.4, 0, .7, 1)));
        slide.KeyFrames.Add(new DiscreteDoubleKeyFrame(-24, TimeSpan.FromMilliseconds(201)));
        slide.KeyFrames.Add(new SplineDoubleKeyFrame(0, TimeSpan.FromMilliseconds(420), new KeySpline(.16, 1, .3, 1)));
        slide.Completed += (_, _) => { if (revision == logoGeneration) ResetLogo(); };
        var fade = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        fade.KeyFrames.Add(new LinearDoubleKeyFrame(1, TimeSpan.Zero));
        fade.KeyFrames.Add(new LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(65)));
        fade.KeyFrames.Add(new SplineDoubleKeyFrame(0, TimeSpan.FromMilliseconds(180), new KeySpline(.4, 0, 1, 1)));
        fade.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(240)));
        fade.KeyFrames.Add(new SplineDoubleKeyFrame(1, TimeSpan.FromMilliseconds(420), new KeySpline(.16, 1, .3, 1)));
        BrandArrow.BeginAnimation(UIElement.OpacityProperty, fade);
        LogoArrowOffset.BeginAnimation(TranslateTransform.YProperty, slide);
    }

    private void ResetLogo()
    {
        logoGeneration++; logoPlaying = false;
        LogoArrowOffset.BeginAnimation(TranslateTransform.YProperty, null);
        BrandArrow.BeginAnimation(UIElement.OpacityProperty, null);
    }
}
