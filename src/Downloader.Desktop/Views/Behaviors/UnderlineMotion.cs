using System.Windows;
using System.Windows.Media;

namespace YouTubeDownloader;

public static class UnderlineMotion
{
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.RegisterAttached(
        "IsSelected", typeof(bool), typeof(UnderlineMotion), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached("State", typeof(State), typeof(UnderlineMotion));
    public static bool GetIsSelected(DependencyObject view) => (bool)view.GetValue(IsSelectedProperty);
    public static void SetIsSelected(DependencyObject view, bool value) => view.SetValue(IsSelectedProperty, value);

    private static void Changed(DependencyObject view, DependencyPropertyChangedEventArgs e)
    {
        if (view is not FrameworkElement line) return;
        if (line.GetValue(StateProperty) is not State state) { state = new State(line); line.SetValue(StateProperty, state); }
        state.Update(line.IsLoaded && line.IsVisible);
    }

    private sealed class State
    {
        private readonly FrameworkElement line;
        private readonly ScaleTransform scale;
        private bool listening;
        private int generation;

        internal State(FrameworkElement line)
        {
            this.line = line;
            scale = (line.RenderTransform as ScaleTransform)?.Clone() ?? new ScaleTransform(0, 1);
            line.RenderTransform = scale;
            line.Loaded += (_, _) => { Subscribe(); Settle(); };
            line.Unloaded += (_, _) => { MotionPreferences.Changed -= Settle; listening = false; Settle(); };
            line.IsVisibleChanged += (_, _) => { if (!line.IsVisible) Settle(); };
            if (line.IsLoaded) Subscribe();
        }

        private void Subscribe() { if (!listening) { MotionPreferences.Changed += Settle; listening = true; } }
        private void Settle() => Update(false);

        internal void Update(bool animate)
        {
            int current = ++generation;
            double from = scale.ScaleX, target = GetIsSelected(line) ? 1 : 0;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null); scale.ScaleX = target;
            if (!animate || !MotionPreferences.Enabled || from == target) return;
            var animation = MotionPreferences.To(target, target > 0 ? 210 : 110); animation.From = from;
            animation.Completed += (_, _) => { if (generation == current) scale.BeginAnimation(ScaleTransform.ScaleXProperty, null); };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        }
    }
}
