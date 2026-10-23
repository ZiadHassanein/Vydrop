using System.Windows;

namespace YouTubeDownloader;

public static class SelectionMotion
{
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.RegisterAttached(
        "IsSelected", typeof(bool), typeof(SelectionMotion), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached("State", typeof(State), typeof(SelectionMotion));
    public static bool GetIsSelected(DependencyObject view) => (bool)view.GetValue(IsSelectedProperty);
    public static void SetIsSelected(DependencyObject view, bool value) => view.SetValue(IsSelectedProperty, value);
    private static void Changed(DependencyObject view, DependencyPropertyChangedEventArgs args)
    {
        if (view is not FrameworkElement layer) return;
        if (layer.GetValue(StateProperty) is not State state) { state = new State(layer); layer.SetValue(StateProperty, state); }
        state.Update(layer.IsLoaded);
    }
    private sealed class State
    {
        private readonly FrameworkElement layer;
        private bool listening;
        private int generation;
        internal State(FrameworkElement layer)
        {
            this.layer = layer;
            layer.Loaded += (_, _) => Subscribe();
            layer.Unloaded += (_, _) => { MotionPreferences.Changed -= Settle; listening = false; Settle(); };
            if (layer.IsLoaded) { MotionPreferences.Changed += Settle; listening = true; }
        }
        private void Subscribe() { if (!listening) { MotionPreferences.Changed += Settle; listening = true; } Settle(); }
        private void Settle() => Update(false);
        internal void Update(bool animate)
        {
            int current = ++generation;
            double from = layer.Opacity, target = GetIsSelected(layer) ? 1 : 0;
            layer.BeginAnimation(UIElement.OpacityProperty, null); layer.Opacity = target;
            if (animate && MotionPreferences.Enabled && from != target)
            {
                var animation = MotionPreferences.To(target, target > 0 ? 170 : 110); animation.From = from;
                animation.Completed += (_, _) => { if (generation == current) layer.BeginAnimation(UIElement.OpacityProperty, null); };
                layer.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }
    }
}
