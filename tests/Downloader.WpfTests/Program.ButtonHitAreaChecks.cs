using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

internal static partial class Program
{
    private static void CheckButtonHitAreas(Window window)
    {
        window.UpdateLayout();
        int checkedButtons = 0;
        foreach (var button in Descendants<ButtonBase>(window).Where(button => button.IsVisible && button.IsEnabled))
        {
            if (button.ActualWidth < 8 || button.ActualHeight < 8) continue;
            var bounds = button.TransformToAncestor(window).TransformBounds(new Rect(button.RenderSize));
            if (!new Rect(window.RenderSize).Contains(bounds)) continue;
            bool clipped = false;
            for (var parent = VisualTreeHelper.GetParent(button); parent != null && parent != window; parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is not FrameworkElement frame || (!frame.ClipToBounds && frame is not ScrollContentPresenter)) continue;
                var localBounds = button.TransformToAncestor(frame).TransformBounds(new Rect(button.RenderSize));
                if (!new Rect(frame.RenderSize).Contains(localBounds)) { clipped = true; break; }
            }
            if (clipped) continue;
            string missedPoint = null;
            foreach (double x in new[] { 4d, button.ActualWidth / 2, button.ActualWidth - 4 })
            foreach (double y in new[] { 4d, button.ActualHeight / 2, button.ActualHeight - 4 })
            {
                var point = button.TranslatePoint(new Point(x, y), window);
                var hit = window.InputHitTest(point) as DependencyObject;
                var owner = hit;
                while (owner != null && owner != button)
                    owner = owner is Visual ? VisualTreeHelper.GetParent(owner) : LogicalTreeHelper.GetParent(owner);
                if (owner != button) missedPoint ??= $"{x:0.##},{y:0.##}; hit={hit?.GetType().Name ?? "none"}";
            }
            string name = System.Windows.Automation.AutomationProperties.GetName(button);
            Check(missedPoint == null, $"Full button surface receives pointer input: {button.GetType().Name}/{button.Name}/{name}/{button.Content}" + (missedPoint == null ? "" : "; missed=" + missedPoint));
            checkedButtons++;
        }
        Check(checkedButtons > 0, "Hit-area checks exercise rendered enabled controls");
    }
}
