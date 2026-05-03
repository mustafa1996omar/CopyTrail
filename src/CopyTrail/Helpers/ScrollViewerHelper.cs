using System.Windows;
using System.Windows.Controls;

namespace CopyTrail.Helpers;

public static class ScrollViewerHelper
{
    public static readonly DependencyProperty HorizontalOffsetProperty =
        DependencyProperty.RegisterAttached(
            "HorizontalOffset",
            typeof(double),
            typeof(ScrollViewerHelper),
            new PropertyMetadata(0.0, OnHorizontalOffsetChanged));

    public static void SetHorizontalOffset(DependencyObject obj, double value)
        => obj.SetValue(HorizontalOffsetProperty, value);

    public static double GetHorizontalOffset(DependencyObject obj)
        => (double)obj.GetValue(HorizontalOffsetProperty);

    private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer sv)
            sv.ScrollToHorizontalOffset((double)e.NewValue);
    }
}
