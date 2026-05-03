using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    public static readonly DependencyProperty ShowNameLabelProperty =
        DependencyProperty.Register(
            nameof(ShowNameLabel),
            typeof(bool),
            typeof(SourceBadge),
            new PropertyMetadata(true, OnShowNameLabelChanged));

    public bool ShowNameLabel
    {
        get => (bool)GetValue(ShowNameLabelProperty);
        set => SetValue(ShowNameLabelProperty, value);
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

    private static void OnShowNameLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SourceBadge badge)
            badge.AppNameText.Visibility = (bool)e.NewValue
                ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Apply(SourceVisualIdentity identity)
    {
        AppNameText.Text = identity.AppName;

        if (!string.IsNullOrEmpty(identity.IconPath) && File.Exists(identity.IconPath))
        {
            try
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(identity.IconPath, UriKind.Absolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.DecodePixelWidth = 24;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                IconImage.Source = bitmapImage;
                IconBorder.Visibility = Visibility.Visible;
                InitialCircle.Visibility = Visibility.Collapsed;
                return;
            }
            catch
            {
                // Fall through to initial-circle fallback.
            }
        }

        // Fallback: colored circle with initial.
        var accentColor = (Color)ColorConverter.ConvertFromString(identity.AccentColorHex);
        InitialCircle.Background = new SolidColorBrush(accentColor);
        InitialText.Text = identity.Initial;
        IconBorder.Visibility = Visibility.Collapsed;
        InitialCircle.Visibility = Visibility.Visible;
    }
}
