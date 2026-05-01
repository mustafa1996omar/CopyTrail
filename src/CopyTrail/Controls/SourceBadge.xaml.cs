using System.Windows;
using System.Windows.Media;
using CopyTrail.Models;

namespace CopyTrail.Controls;

public partial class SourceBadge
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source), typeof(SourceVisualIdentity),
            typeof(SourceBadge), new PropertyMetadata(null, OnSourceChanged));

    public SourceVisualIdentity? Source
    {
        get => (SourceVisualIdentity?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public SourceBadge()
    {
        InitializeComponent();
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SourceBadge badge && e.NewValue is SourceVisualIdentity identity)
            badge.Apply(identity);
    }

    private void Apply(SourceVisualIdentity identity)
    {
        var color = (Color)ColorConverter.ConvertFromString(identity.AccentColorHex);
        InitialCircle.Background = new SolidColorBrush(color);
        InitialText.Text = identity.Initial;
        AppNameText.Text = identity.AppName;
    }
}
