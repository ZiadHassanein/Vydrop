using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Runtime.CompilerServices;

namespace YouTubeDownloader;

internal static class ViewMotion
{
    private static readonly ConditionalWeakTable<FrameworkElement, State> States = new();
    internal static void Reveal(FrameworkElement view) => Play(view, 0, 4);
    internal static void Navigate(FrameworkElement view, int direction) => Play(view, 12 * direction, 0);
    internal static void Stop(FrameworkElement view) { if (view != null && States.TryGetValue(view, out var state)) state.Stop(); }
    private static void Play(FrameworkElement view, double x, double y)
    {
        if (!MotionPreferences.Enabled) return;
        States.GetValue(view, element => new State(element)).Play(x, y);
    }
    private sealed class State
    {
        private readonly FrameworkElement view;
        private Transform original;
        private TransformGroup group;
        private TranslateTransform offset;
        private int generation;
        internal State(FrameworkElement view) { this.view = view; view.Unloaded += (_, _) => Stop(); view.IsVisibleChanged += (_, _) => { if (!view.IsVisible) Stop(); }; }
        internal void Play(double x, double y)
        {
            Stop();
            int current = ++generation;
            original = view.RenderTransform;
            offset = new TranslateTransform(); group = new TransformGroup(); group.Children.Add(original); group.Children.Add(offset);
            view.RenderTransform = group;
            MotionPreferences.Changed += Stop;
            var fade = new DoubleAnimation(.94, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop };
            fade.Completed += (_, _) => { if (generation == current) Stop(); };
            view.BeginAnimation(UIElement.OpacityProperty, fade);
            offset.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, 0, fade.Duration) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop });
            offset.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(y, 0, fade.Duration) { EasingFunction = MotionPreferences.EaseOut, FillBehavior = FillBehavior.Stop });
        }
        internal void Stop()
        {
            generation++;
            MotionPreferences.Changed -= Stop;
            view.BeginAnimation(UIElement.OpacityProperty, null);
            offset?.BeginAnimation(TranslateTransform.XProperty, null); offset?.BeginAnimation(TranslateTransform.YProperty, null);
            if (ReferenceEquals(view.RenderTransform, group)) view.RenderTransform = original;
        }
    }
}
