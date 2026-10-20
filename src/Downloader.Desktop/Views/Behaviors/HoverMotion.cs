using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YouTubeDownloader;

public static class HoverMotion
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(HoverMotion), new PropertyMetadata(false, Changed));
    public static bool GetEnabled(DependencyObject view) => (bool)view.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject view, bool value) => view.SetValue(EnabledProperty, value);
    public static readonly DependencyProperty StrengthProperty = DependencyProperty.RegisterAttached(
        "Strength", typeof(double), typeof(HoverMotion), new PropertyMetadata(1d));
    public static double GetStrength(DependencyObject view) => (double)view.GetValue(StrengthProperty);
    public static void SetStrength(DependencyObject view, double value) => view.SetValue(StrengthProperty, value);
    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(HoverState), typeof(HoverMotion));

    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is not FrameworkElement layer) return;
        if (layer.GetValue(StateProperty) is HoverState previous) previous.Dispose();
        layer.SetValue(StateProperty, (bool)args.NewValue ? new HoverState(layer) : null);
    }

    private sealed class HoverState : IDisposable
    {
        private readonly FrameworkElement layer;
        private Control owner;
        private bool hovered;

        internal HoverState(FrameworkElement layer)
        {
            this.layer = layer;
            layer.Loaded += Loaded;
            layer.Unloaded += Unloaded;
            if (layer.IsLoaded) Loaded(layer, new RoutedEventArgs());
        }

        private void Loaded(object sender, RoutedEventArgs args)
        {
            if (owner != null || layer.TemplatedParent is not Control control) return;
            owner = control;
            hovered = owner.IsMouseOver;
            owner.MouseEnter += Entered;
            owner.MouseLeave += Left;
            owner.IsEnabledChanged += StateChanged;
            owner.IsVisibleChanged += StateChanged;
            MotionPreferences.Changed += Settle;
            Settle();
        }

        private void Entered(object sender, MouseEventArgs args) { hovered = true; Update(true); }
        private void Left(object sender, MouseEventArgs args) { hovered = false; Update(true); }
        private void StateChanged(object sender, DependencyPropertyChangedEventArgs args) => Update(false);
        private void Settle() => Update(false);

        private void Update(bool animate)
        {
            double target = hovered && owner is { IsEnabled: true, IsVisible: true } ? GetStrength(layer) : 0;
            double from = layer.Opacity;
            layer.BeginAnimation(UIElement.OpacityProperty, null);
            layer.Opacity = target;
            if (animate && MotionPreferences.Enabled && from != target)
            {
                var motion = MotionPreferences.To(target, target > 0 ? 130 : 100);
                motion.From = from;
                layer.BeginAnimation(UIElement.OpacityProperty, motion);
            }
        }

        private void Unloaded(object sender, RoutedEventArgs args)
        {
            if (owner == null) return;
            owner.MouseEnter -= Entered;
            owner.MouseLeave -= Left;
            hovered = false;
            owner.IsEnabledChanged -= StateChanged;
            owner.IsVisibleChanged -= StateChanged;
            MotionPreferences.Changed -= Settle;
            owner = null;
            layer.BeginAnimation(UIElement.OpacityProperty, null);
            layer.Opacity = 0;
        }

        public void Dispose()
        {
            Unloaded(layer, new RoutedEventArgs());
            layer.Loaded -= Loaded;
            layer.Unloaded -= Unloaded;
        }
    }
}
