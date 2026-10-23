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
        IsVisibleChanged += (_, _) => { if (!IsVisible) ResetLogo(); };
        BrandButton.IsVisibleChanged += (_, _) => { if (!BrandButton.IsVisible) ResetLogo(); };
        BrandButton.IsEnabledChanged += (_, _) => { if (!BrandButton.IsEnabled) ResetLogo(); };
    }

    private void LogoClicked(object sender, RoutedEventArgs args) => PlayLogo();
    private void LogoHovered(object sender, MouseEventArgs args) => PlayLogo();

    private void PlayLogo()
    {
        if (logoPlaying || !IsLoaded || !BrandButton.IsVisible || !BrandButton.IsEnabled || !MotionPreferences.Enabled) return;
        ResetLogo(); logoPlaying = true;
        long revision = logoGeneration;
        var turn = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(540))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop
        };
        var counterTurn = turn.Clone(); counterTurn.To = -360;
        turn.Completed += (_, _) =>
        {
            if (logoGeneration != revision) return;
            SettleTurn();
            var drop = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
            drop.KeyFrames.Add(new LinearDoubleKeyFrame(0, TimeSpan.Zero));
            drop.KeyFrames.Add(new SplineDoubleKeyFrame(40, TimeSpan.FromMilliseconds(90), new KeySpline(.4, 0, .7, 1)));
            drop.KeyFrames.Add(new SplineDoubleKeyFrame(-12, TimeSpan.FromMilliseconds(175), new KeySpline(.16, 1, .3, 1)));
            drop.KeyFrames.Add(new SplineDoubleKeyFrame(0, TimeSpan.FromMilliseconds(260), new KeySpline(.22, 1, .36, 1)));
            drop.Completed += (_, _) => { if (logoGeneration == revision) ResetLogo(); };
            ArrowOffset.BeginAnimation(TranslateTransform.YProperty, drop);
        };
        var scale = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
        foreach (var (milliseconds, value) in new[] { (0, 1d), (90, .8), (450, .8), (540, 1d) })
            scale.KeyFrames.Add(new SplineDoubleKeyFrame(value, TimeSpan.FromMilliseconds(milliseconds), new KeySpline(.37, 0, .63, 1)));
        foreach (var transform in new[] { LogoScale, ArrowScale })
        {
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
        }
        ArrowTurn.BeginAnimation(RotateTransform.AngleProperty, counterTurn);
        LogoTurn.BeginAnimation(RotateTransform.AngleProperty, turn);
    }

    private void ResetLogo()
    {
        logoGeneration++; logoPlaying = false;
        SettleTurn();
        ArrowOffset.BeginAnimation(TranslateTransform.YProperty, null);
    }

    private void SettleTurn()
    {
        LogoTurn.BeginAnimation(RotateTransform.AngleProperty, null);
        ArrowTurn.BeginAnimation(RotateTransform.AngleProperty, null);
        foreach (var transform in new[] { LogoScale, ArrowScale })
        {
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }
    }

}
