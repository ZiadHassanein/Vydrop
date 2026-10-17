using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YouTubeDownloader;

public partial class DownloadActionView : UserControl
{
    private MainViewModel model;
    private DownloadActionState previous;
    private double previousProgress;
    private ScaleTransform progressScale;
    private ShineMotion shine;

    public DownloadActionView()
    {
        InitializeComponent();
        Loaded += LoadedView;
        Unloaded += UnloadedView;
        DataContextChanged += (_, _) => Rebind();
        IsVisibleChanged += (_, _) => { if (!IsVisible) shine?.Stop(); Refresh(false); };
    }

    private void LoadedView(object sender, RoutedEventArgs args)
    {
        MotionPreferences.Changed += Settle;
        Rebind();
    }

    private void Rebind()
    {
        if (model != null) model.PropertyChanged -= ModelChanged;
        model = IsLoaded ? DataContext as MainViewModel : null;
        if (model != null) model.PropertyChanged += ModelChanged;
        DownloadButton.ApplyTemplate();
        progressScale = DownloadButton.Template.FindName("ProgressScale", DownloadButton) as ScaleTransform;
        shine?.Stop();
        shine = null;
        var layer = DownloadButton.Template.FindName("DownloadShine", DownloadButton) as FrameworkElement;
        var surface = DownloadButton.Template.FindName("DownloadShineSurface", DownloadButton) as Border;
        if (layer != null && surface?.OpacityMask is LinearGradientBrush brush)
        {
            var light = brush.Clone();
            surface.OpacityMask = light;
            shine = new ShineMotion(layer, (TranslateTransform)light.RelativeTransform);
        }
        Refresh(false);
    }

    private void ModelChanged(object sender, PropertyChangedEventArgs args)
    {
        if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName is nameof(MainViewModel.DownloadActionState) or nameof(MainViewModel.IsCheckingMedia) or nameof(MainViewModel.Progress) or nameof(MainViewModel.Indeterminate))
            Refresh(true);
    }

    private void DownloadHovered(object sender, MouseEventArgs args) => PlayShine();
    private void DownloadClicked(object sender, RoutedEventArgs args) => PlayShine();
    private void DownloadLeft(object sender, MouseEventArgs args)
    {
        // An accepted click disables the action while downloading; let its
        // one-shot feedback finish instead of treating that as a pointer exit.
        if (model?.DownloadActionState != DownloadActionState.Downloading) shine?.FadeOut();
    }
    private void PlayShine()
    {
        if (IsLoaded && IsVisible && DownloadButton.IsEnabled) shine?.Play();
    }

    private void Settle() { shine?.Stop(); Refresh(false); }

    private void Refresh(bool animate)
    {
        if (progressScale == null) return;
        var phase = model?.DownloadActionState ?? DownloadActionState.Ready;
        bool checking = model?.IsCheckingMedia == true;
        bool scanMotion = IsLoaded && IsVisible && MotionPreferences.Enabled;
        if (!checking || !scanMotion) CheckScanOffset.BeginAnimation(TranslateTransform.XProperty, null);
        else if (!CheckScanOffset.HasAnimatedProperties)
            CheckScanOffset.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(3, 17, TimeSpan.FromMilliseconds(1100))
                { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        CheckingVideo.Visibility = checking ? Visibility.Visible : Visibility.Hidden;
        bool motion = animate && IsLoaded && IsVisible && MotionPreferences.Enabled;
        double value = phase == DownloadActionState.Downloading && model is { Indeterminate: false }
            ? Math.Clamp(model.Progress / 100, 0, 1) : phase == DownloadActionState.Complete ? 1 : 0;
        double from = progressScale.ScaleX;
        progressScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        progressScale.ScaleX = value;
        if (motion && phase == previous && value > previousProgress)
        {
            var progress = MotionPreferences.To(value, 180);
            progress.From = from;
            progressScale.BeginAnimation(ScaleTransform.ScaleXProperty, progress);
        }
        if (!motion || phase != previous)
        {
            ArrowOffset.BeginAnimation(TranslateTransform.YProperty, null);
            CompleteCheck.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty, null);
            CompleteCheck.BeginAnimation(OpacityProperty, null);
            CompletionScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            CompletionScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }
        DownloadArrow.Visibility = checking || phase == DownloadActionState.Complete ? Visibility.Hidden : Visibility.Visible;
        CompleteCheck.Visibility = phase == DownloadActionState.Complete ? Visibility.Visible : Visibility.Hidden;
        if (phase != previous && phase == DownloadActionState.Downloading && IsLoaded && IsVisible)
            _ = Dispatcher.InvokeAsync(() =>
            {
                if (IsVisible && model?.DownloadActionState == DownloadActionState.Downloading) BringIntoView();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        if (motion && phase != previous)
        {
            if (phase == DownloadActionState.Downloading)
            {
                var arrow = new DoubleAnimationUsingKeyFrames { FillBehavior = FillBehavior.Stop };
                arrow.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.Zero));
                arrow.KeyFrames.Add(new EasingDoubleKeyFrame(5, TimeSpan.FromMilliseconds(110), MotionPreferences.EaseOut));
                arrow.KeyFrames.Add(new EasingDoubleKeyFrame(0, TimeSpan.FromMilliseconds(250), MotionPreferences.EaseOut));
                ArrowOffset.BeginAnimation(TranslateTransform.YProperty, arrow);
            }
            else if (phase == DownloadActionState.Complete)
            {
                CompleteCheck.BeginAnimation(System.Windows.Shapes.Shape.StrokeDashOffsetProperty,
                    new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(480)) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }, FillBehavior = FillBehavior.Stop });
                CompleteCheck.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)) { FillBehavior = FillBehavior.Stop });
                var settle = new DoubleAnimation(.88, 1, TimeSpan.FromMilliseconds(480)) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop };
                CompletionScale.BeginAnimation(ScaleTransform.ScaleXProperty, settle);
                CompletionScale.BeginAnimation(ScaleTransform.ScaleYProperty, settle);
            }
        }
        previous = phase;
        previousProgress = value;
    }

    private void UnloadedView(object sender, RoutedEventArgs args)
    {
        MotionPreferences.Changed -= Settle;
        shine?.Stop();
        if (model != null) model.PropertyChanged -= ModelChanged;
        model = null;
        Refresh(false);
    }
}
