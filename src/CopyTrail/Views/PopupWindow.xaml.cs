using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using CopyTrail.Helpers;
using CopyTrail.Services;
using CopyTrail.ViewModels;
using WinInput = System.Windows.Input;

namespace CopyTrail.Views;

public partial class PopupWindow
{
    private readonly IntPtr _previousWindow;
    private bool _isPasting;
    private bool _isClosing;

    public PopupWindow(IntPtr previousWindow = default)
    {
        _previousWindow = previousWindow;
        InitializeComponent();
        DataContext = new PopupViewModel(App.Repository, App.Settings, App.CleanupService);
    }

    private PopupViewModel ViewModel => (PopupViewModel)DataContext;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PositionNearCursor();
        PausedBanner.Visibility = App.IsCaptureActive ? Visibility.Collapsed : Visibility.Visible;
        _ = ViewModel.LoadAsync();
    }

    internal void RefreshAsync()
    {
        _ = ViewModel.LoadAsync();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(160)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(OpacityProperty, fadeIn);
        SearchBox.Focus();
    }

    protected override void OnKeyDown(WinInput.KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == WinInput.Key.Escape)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Clear();
                ViewModel.SearchText = "";
            }
            else
            {
                AnimatedClose();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == WinInput.Key.F && WinInput.Keyboard.Modifiers == WinInput.ModifierKeys.Control)
        {
            SearchBox.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == WinInput.Key.Up)
        {
            ViewModel.SelectPrevious();
            ScrollSelectedIntoView();
            e.Handled = true;
            return;
        }

        if (e.Key == WinInput.Key.Down)
        {
            ViewModel.SelectNext();
            ScrollSelectedIntoView();
            e.Handled = true;
            return;
        }

        if (e.Key == WinInput.Key.Enter)
        {
            if (WinInput.Keyboard.Modifiers == WinInput.ModifierKeys.Control)
            {
                ExecuteCopyOnly(ViewModel.SelectedCard);
            }
            else
            {
                _ = ExecutePasteAsync(ViewModel.SelectedCard);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == WinInput.Key.Delete && !SearchBox.IsFocused)
        {
            var selected = ViewModel.SelectedCard;
            if (selected is not null)
            {
                _ = ViewModel.DeleteCardAsync(selected);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == WinInput.Key.P && !SearchBox.IsFocused)
        {
            var selected = ViewModel.SelectedCard;
            if (selected is not null)
            {
                _ = ViewModel.TogglePinAsync(selected);
                e.Handled = true;
            }
            return;
        }
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (_isPasting) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsActive)
                Close();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Called by ClipCard when the user clicks a card. Triggers paste.
    /// </summary>
    internal void RequestPaste(ClipCardViewModel vm)
    {
        ViewModel.SetSelectedCard(vm);
        _ = ExecutePasteAsync(vm);
    }

    /// <summary>
    /// Called by ClipCard when the user clicks the Copy action button.
    /// Copies item to clipboard without pasting and closes the popup.
    /// </summary>
    internal void RequestCopy(ClipCardViewModel vm)
    {
        ViewModel.SetSelectedCard(vm);
        ExecuteCopyOnly(vm);
    }

    internal void RequestTogglePin(ClipCardViewModel vm)
    {
        _ = ViewModel.TogglePinAsync(vm);
    }

    internal void RequestDelete(ClipCardViewModel vm)
    {
        _ = ViewModel.DeleteCardAsync(vm);
    }

    private async Task ExecutePasteAsync(ClipCardViewModel? vm)
    {
        if (vm?.Content is null) return;
        if (_isPasting) return;

        var pasteService = App.PasteService;
        if (pasteService is null) return;

        _isPasting = true;
        HideError();

        bool success = await pasteService.PasteAsync(vm.Content, _previousWindow, () => Close());
        _isPasting = false;

        if (!success)
            ShowError("Could not paste this item. The content may be unavailable.");
    }

    private void ExecuteCopyOnly(ClipCardViewModel? vm)
    {
        if (vm?.Content is null) return;

        var pasteService = App.PasteService;
        if (pasteService is null) return;

        HideError();
        bool success = pasteService.CopyOnly(vm.Content);

        if (success)
            AnimatedClose();
        else
            ShowError("Could not copy this item. The content may be unavailable.");
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        ErrorText.Text = "";
    }

    private void AnimatedClose()
    {
        if (_isClosing) return;
        _isClosing = true;
        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(150)))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void ScrollSelectedIntoView()
    {
        var vm = ViewModel.SelectedCard;
        if (vm is null) return;
        var container = TryGetContainer(PinnedItemsControl, vm)
                     ?? TryGetContainer(RegularItemsControl, vm);
        container?.BringIntoView();
    }

    private static FrameworkElement? TryGetContainer(System.Windows.Controls.ItemsControl ic, object item)
        => ic.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SearchClearButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        SearchBox.Focus();
    }

    private void SelectChip(FilterKind kind)
    {
        ViewModel.SelectFilter(kind);
        SetChipSelected(ChipBorderAll, ChipTextAll, kind == FilterKind.All);
        SetChipSelected(ChipBorderText, ChipTextText, kind == FilterKind.Text);
        SetChipSelected(ChipBorderLinks, ChipTextLinks, kind == FilterKind.Links);
        SetChipSelected(ChipBorderCode, ChipTextCode, kind == FilterKind.Code);
        SetChipSelected(ChipBorderImages, ChipTextImages, kind == FilterKind.Images);
        SetChipSelected(ChipBorderFiles, ChipTextFiles, kind == FilterKind.Files);
        SetChipSelected(ChipBorderColors, ChipTextColors, kind == FilterKind.Colors);
        SetChipSelected(ChipBorderPinned, ChipTextPinned, kind == FilterKind.Pinned);
    }

    private void SetChipSelected(Border border, TextBlock text, bool selected)
    {
        border.Style = (Style)FindResource(selected ? "ChipSelectedStyle" : "ChipStyle");
        text.Style = (Style)FindResource(selected ? "ChipTextSelectedStyle" : "ChipTextStyle");
    }

    private void SettingsButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        AnimatedClose();
        App.OpenSettings();
    }

    private void ChipAll_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.All);
    private void ChipText_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Text);
    private void ChipLinks_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Links);
    private void ChipCode_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Code);
    private void ChipImages_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Images);
    private void ChipFiles_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Files);
    private void ChipColors_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Colors);
    private void ChipPinned_MouseUp(object sender, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Pinned);

    private void PositionNearCursor()
    {
        if (!Win32Helpers.GetCursorPos(out var cursor))
            return;

        var hMonitor = Win32Helpers.MonitorFromPoint(cursor, Win32Helpers.MONITOR_DEFAULTTONEAREST);
        var mi = new Win32Helpers.MONITORINFO
        {
            cbSize = (uint)Marshal.SizeOf<Win32Helpers.MONITORINFO>()
        };
        if (!Win32Helpers.GetMonitorInfo(hMonitor, ref mi))
            return;

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget == null)
            return;

        var fromDevice = source.CompositionTarget.TransformFromDevice;

        var cursorDip = fromDevice.Transform(new System.Windows.Point(cursor.X, cursor.Y));
        var workTL = fromDevice.Transform(new System.Windows.Point(mi.rcWork.left, mi.rcWork.top));
        var workBR = fromDevice.Transform(new System.Windows.Point(mi.rcWork.right, mi.rcWork.bottom));

        double left = cursorDip.X + 16;
        double top = cursorDip.Y + 16;

        if (left + Width > workBR.X)
            left = workBR.X - Width;
        if (top + Height > workBR.Y)
            top = workBR.Y - Height;
        if (left < workTL.X)
            left = workTL.X;
        if (top < workTL.Y)
            top = workTL.Y;

        Left = left;
        Top = top;
    }
}
