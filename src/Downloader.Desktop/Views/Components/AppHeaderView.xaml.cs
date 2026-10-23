using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

public partial class AppHeaderView : UserControl
{
    private readonly ShineMotion shine;

    public AppHeaderView()
    {
        InitializeComponent();
        var light = ((LinearGradientBrush)LogoLight.Fill).Clone();
        LogoLight.Fill = light;
        shine = new ShineMotion(LogoLight, (TranslateTransform)light.RelativeTransform);
        Loaded += (_, _) => { MotionPreferences.Changed += ResetLogo; };
        Unloaded += (_, _) => { MotionPreferences.Changed -= ResetLogo; ResetLogo(); };
        IsVisibleChanged += (_, _) => { if (!IsVisible) ResetLogo(); };
    }

    private void LogoClicked(object sender, RoutedEventArgs args) => PlayLogo(pulse: true);
    private void LogoHovered(object sender, MouseEventArgs args) => PlayLogo();
    private void LogoLeft(object sender, MouseEventArgs args) => shine.FadeOut(ResetLogo);

    private void PlayLogo(bool pulse = false)
    {
        ResetLogo();
        if (!IsLoaded || !IsVisible || !BrandButton.IsEnabled || !MotionPreferences.Enabled) return;
        if (pulse)
        {
            LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.94, 1, TimeSpan.FromMilliseconds(360)) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop });
            LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.94, 1, TimeSpan.FromMilliseconds(360)) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop });
        }
        shine.Play();
    }

    private void ResetLogo()
    {
        shine.Stop();
        LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    }

}
