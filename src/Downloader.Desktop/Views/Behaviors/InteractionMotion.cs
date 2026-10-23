using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace YouTubeDownloader;

public static class InteractionMotion
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(InteractionMotion), new PropertyMetadata(false, Changed));
    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached("State", typeof(State), typeof(InteractionMotion));
    public static bool GetEnabled(DependencyObject view) => (bool)view.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject view, bool value) => view.SetValue(EnabledProperty, value);
    private static void Changed(DependencyObject view, DependencyPropertyChangedEventArgs args)
    {
        if (view is not FrameworkElement visual) return;
        (visual.GetValue(StateProperty) as State)?.Dispose();
        visual.SetValue(StateProperty, (bool)args.NewValue ? new State(visual) : null);
    }

    private sealed class State : IDisposable
    {
        private readonly FrameworkElement visual;
        private readonly ScaleTransform scale = new();
        private ButtonBase owner;
        private Transform original;
        private Point origin;
        private TransformGroup group;
        private DependencyPropertyDescriptor pressed;
        private int generation;
        internal State(FrameworkElement visual)
        {
            this.visual = visual;
            visual.Loaded += Loaded; visual.Unloaded += Unloaded;
            if (visual.IsLoaded) Loaded(visual, new RoutedEventArgs());
        }
        private void Loaded(object sender, RoutedEventArgs args)
        {
            if (owner != null || visual.TemplatedParent is not ButtonBase button) return;
            owner = button; original = visual.RenderTransform; origin = visual.RenderTransformOrigin;
            group = new TransformGroup(); group.Children.Add(original); group.Children.Add(scale);
            visual.RenderTransform = group; visual.RenderTransformOrigin = new Point(.5, .5);
            pressed = DependencyPropertyDescriptor.FromProperty(ButtonBase.IsPressedProperty, owner.GetType());
            pressed.AddValueChanged(owner, Pressed);
            owner.Click += Clicked; owner.IsEnabledChanged += Settled; owner.IsVisibleChanged += Settled;
            MotionPreferences.Changed += Reset;
            Reset();
        }
        private void Pressed(object sender, EventArgs args) => Animate(owner.IsPressed ? .975 : 1, owner.IsPressed ? 80 : 140);
        private void Clicked(object sender, RoutedEventArgs args)
        {
            if (owner.IsEnabled && MotionPreferences.Enabled)
            {
                scale.ScaleX = scale.ScaleY = .975;
                Animate(1, 150);
            }
        }
        private void Settled(object sender, DependencyPropertyChangedEventArgs args) => Reset();
        private void Animate(double target, double duration)
        {
            int current = ++generation;
            double from = scale.ScaleX;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null); scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleX = scale.ScaleY = target;
            if (!MotionPreferences.Enabled || owner is not { IsEnabled: true, IsVisible: true }) { Reset(); return; }
            var animation = MotionPreferences.To(target, duration); animation.From = from;
            animation.Completed += (_, _) =>
            {
                if (generation != current) return;
                generation++;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null); scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                scale.ScaleX = scale.ScaleY = target;
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }
        private void Reset()
        {
            generation++;
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null); scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleX = scale.ScaleY = 1;
        }
        private void Unloaded(object sender, RoutedEventArgs args)
        {
            if (owner == null) return;
            pressed.RemoveValueChanged(owner, Pressed);
            owner.Click -= Clicked; owner.IsEnabledChanged -= Settled; owner.IsVisibleChanged -= Settled;
            MotionPreferences.Changed -= Reset; Reset();
            if (ReferenceEquals(visual.RenderTransform, group)) visual.RenderTransform = original;
            visual.RenderTransformOrigin = origin; owner = null;
        }
        public void Dispose() { Unloaded(visual, new RoutedEventArgs()); visual.Loaded -= Loaded; visual.Unloaded -= Unloaded; }
    }
}
