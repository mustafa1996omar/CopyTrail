using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using WinInput = System.Windows.Input;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using CopyTrail.ViewModels;
using CopyTrail.Views;

namespace CopyTrail.Controls;

public partial class ClipCard
{
    private static readonly SolidColorBrush DefaultBorder =
        new(Color.FromRgb(0xE5, 0xE7, 0xEB));

    private static readonly SolidColorBrush SelectedBorder =
        new(Color.FromRgb(0x25, 0x63, 0xEB));

    public ClipCard()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ClipCardViewModel oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is ClipCardViewModel newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClipCardViewModel.IsSelected))
            ApplySelectionVisual();
    }

    private void ApplySelectionVisual()
    {
        if (DataContext is not ClipCardViewModel vm) return;

        if (vm.IsSelected)
        {
            CardBorder.BorderBrush = SelectedBorder;
            CardBorder.BorderThickness = new Thickness(2);
            CardBorder.Effect = new DropShadowEffect
            {
                BlurRadius = 24,
                Direction = 270,
                ShadowDepth = 6,
                Opacity = 0.18,
                Color = Color.FromRgb(0x25, 0x63, 0xEB)
            };
        }
        else
        {
            CardBorder.BorderBrush = DefaultBorder;
            CardBorder.BorderThickness = new Thickness(1.5);
            CardBorder.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                Direction = 270,
                ShadowDepth = 3,
                Opacity = 0.08,
                Color = Colors.Black
            };
        }
    }

    protected override void OnMouseEnter(WinInput.MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (DataContext is ClipCardViewModel vm && !vm.IsSelected)
        {
            CardBorder.BorderBrush = vm.AccentBrush;
            CardBorder.Effect = new DropShadowEffect
            {
                BlurRadius = 22,
                Direction = 270,
                ShadowDepth = 5,
                Opacity = 0.13,
                Color = Colors.Black
            };
        }
    }

    protected override void OnMouseLeave(WinInput.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ApplySelectionVisual();
    }

    protected override void OnMouseLeftButtonUp(WinInput.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        e.Handled = true;

        if (DataContext is not ClipCardViewModel vm) return;
        var popup = Window.GetWindow(this) as PopupWindow;
        popup?.RequestPaste(vm);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (DataContext is not ClipCardViewModel vm) return;

        var menu = new WpfContextMenu();

        var ignoreItem = new WpfMenuItem { Header = "Ignore this app next time" };
        ignoreItem.IsEnabled = !string.IsNullOrWhiteSpace(vm.SourceProcessName);
        ignoreItem.Click += (_, _) => AddToExclusionList(vm.SourceProcessName!);
        menu.Items.Add(ignoreItem);

        menu.PlacementTarget = (UIElement)sender;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private static void AddToExclusionList(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;

        var settings = App.Settings;
        string normalizedNew = StripExe(processName);
        bool alreadyExcluded = settings.ExcludedProcessNames.Any(
            e => string.Equals(StripExe(e), normalizedNew, StringComparison.OrdinalIgnoreCase));

        if (!alreadyExcluded)
        {
            settings.ExcludedProcessNames.Add(processName);
            App.SettingsService.Save();
        }
    }

    private static string StripExe(string name) =>
        name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
}
