using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CopyTrail.Helpers;
using CopyTrail.ViewModels;
using WinInput = System.Windows.Input;

namespace CopyTrail.Views;

public partial class PopupWindow : Window
{
    private IntPtr _previousWindow;
    private bool _isPasting;
    private bool _isAnimating;
    private bool _forceClosing;
    private DateTime _lastShownAt;
    private const double ShowMs = 240;
    private const double HideMs = 180;

    public PopupWindow(IntPtr previousWindow = default)
    {
        _previousWindow = previousWindow;
        InitializeComponent();
        DataContext = new PopupViewModel(App.Repository, App.Settings, App.CleanupService);
    }

    public void Initialize()
    {
        Opacity = 0;
        SlideTransform.Y = 9999;
        Show();
        PositionAtScreenBottom();
        SlideTransform.Y = Height;
        Hide();
    }

    public void UpdatePreviousWindow(IntPtr hwnd) => _previousWindow = hwnd;

    public void AnimatedShow()
    {
        if (_isAnimating) return;

        PausedBanner.Visibility = App.IsCaptureActive
            ? Visibility.Collapsed : Visibility.Visible;
        HideError();
        _ = ViewModel.LoadAsync();

        SlideTransform.Y = Height;
        Opacity = 0;
        Show();
        PositionAtScreenBottom();
        Activate();

        ApplyChipStyles();

        _lastShownAt = DateTime.UtcNow;
        _isAnimating = true;

        var slide = new DoubleAnimation(Height, 0,
            new Duration(TimeSpan.FromMilliseconds(ShowMs)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        slide.Completed += (_, _) => _isAnimating = false;
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, slide);

        var fade = new DoubleAnimation(0, 1,
            new Duration(TimeSpan.FromMilliseconds(80)));
        this.BeginAnimation(OpacityProperty, fade);
    }

    public void AnimatedHide(Action? onComplete = null)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        var slide = new DoubleAnimation(0, Height,
            new Duration(TimeSpan.FromMilliseconds(HideMs)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        slide.Completed += (_, _) =>
        {
            _isAnimating = false;
            Hide();
            onComplete?.Invoke();
        };
        SlideTransform.BeginAnimation(TranslateTransform.YProperty, slide);

        var fade = new DoubleAnimation(1, 0,
            new Duration(TimeSpan.FromMilliseconds(60)))
        { BeginTime = TimeSpan.FromMilliseconds(120) };
        this.BeginAnimation(OpacityProperty, fade);
    }

    public void ForceClose()
    {
        _forceClosing = true;
        Close();
    }

    private PopupViewModel ViewModel => (PopupViewModel)DataContext;

    private void PositionAtScreenBottom()
    {
        if (!Win32Helpers.GetCursorPos(out var cursor))
            cursor = default;

        var hMonitor = Win32Helpers.MonitorFromPoint(cursor, Win32Helpers.MONITOR_DEFAULTTONEAREST);
        var mi = new Win32Helpers.MONITORINFO
        {
            cbSize = (uint)Marshal.SizeOf<Win32Helpers.MONITORINFO>()
        };
        Win32Helpers.GetMonitorInfo(hMonitor, ref mi);

        var source = PresentationSource.FromVisual(this);
        double scaleX = 1, scaleY = 1;
        if (source?.CompositionTarget != null)
        {
            scaleX = source.CompositionTarget.TransformFromDevice.M11;
            scaleY = source.CompositionTarget.TransformFromDevice.M22;
        }

        double monitorLeft   = mi.rcMonitor.left   * scaleX;
        double monitorBottom = mi.rcMonitor.bottom * scaleY;
        double monitorWidth  = (mi.rcMonitor.right - mi.rcMonitor.left) * scaleX;

        Width = monitorWidth;
        Left  = monitorLeft;
        Top   = monitorBottom - Height;
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (_isPasting || _forceClosing) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsActive && IsVisible && (DateTime.UtcNow - _lastShownAt).TotalMilliseconds > 500)
                AnimatedHide();
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    protected override void OnKeyDown(WinInput.KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case WinInput.Key.Escape:
                AnimatedHide();
                e.Handled = true;
                return;

            case WinInput.Key.F when WinInput.Keyboard.Modifiers == WinInput.ModifierKeys.Control:
                FocusSearch();
                e.Handled = true;
                return;

            case WinInput.Key.Right:
                ViewModel.SelectNext();
                ScrollSelectedIntoView();
                e.Handled = true;
                return;

            case WinInput.Key.Left:
                ViewModel.SelectPrevious();
                ScrollSelectedIntoView();
                e.Handled = true;
                return;

            case WinInput.Key.Enter:
                if (WinInput.Keyboard.Modifiers == WinInput.ModifierKeys.Control)
                    ExecuteCopyOnly(ViewModel.SelectedCard);
                else
                    _ = ExecutePasteAsync(ViewModel.SelectedCard);
                e.Handled = true;
                return;

            case WinInput.Key.Delete when !IsSearchFocused():
                var del = ViewModel.SelectedCard;
                if (del is not null) _ = ViewModel.DeleteCardAsync(del);
                e.Handled = true;
                return;

            case WinInput.Key.P when !IsSearchFocused():
                var pin = ViewModel.SelectedCard;
                if (pin is not null) _ = ViewModel.TogglePinAsync(pin);
                e.Handled = true;
                return;
        }
    }

    internal void RequestPaste(ClipCardViewModel vm)
    {
        ViewModel.SetSelectedCard(vm);
        _ = ExecutePasteAsync(vm);
    }

    internal void RequestCopy(ClipCardViewModel vm)
    {
        ViewModel.SetSelectedCard(vm);
        ExecuteCopyOnly(vm);
    }

    internal void RequestTogglePin(ClipCardViewModel vm) => _ = ViewModel.TogglePinAsync(vm);
    internal void RequestDelete(ClipCardViewModel vm) => _ = ViewModel.DeleteCardAsync(vm);

    private async Task ExecutePasteAsync(ClipCardViewModel? vm)
    {
        if (vm?.Content is null || _isPasting) return;
        var pasteService = App.PasteService;
        if (pasteService is null) return;

        _isPasting = true;
        HideError();

        bool success = await pasteService.PasteAsync(vm.Content, _previousWindow,
            () => AnimatedHide());
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
            AnimatedHide();
        else
            ShowError("Could not copy this item.");
    }

    internal void RefreshAsync() => _ = ViewModel.LoadAsync();

    private void ShowError(string msg) { ErrorText.Text = msg; ErrorBanner.Visibility = Visibility.Visible; }
    private void HideError() { ErrorBanner.Visibility = Visibility.Collapsed; }

    // ── Chip styling ──────────────────────────────────────────────────────────

    private void ApplyChipStyles()
    {
        var filter = ViewModel.SelectedFilter;
        ApplyChip(ChipBorderAll,    ChipTextAll,    filter == FilterKind.All);
        ApplyChip(ChipBorderText,   ChipTextText,   filter == FilterKind.Text);
        ApplyChip(ChipBorderLinks,  ChipTextLinks,  filter == FilterKind.Links);
        ApplyChip(ChipBorderCode,   ChipTextCode,   filter == FilterKind.Code);
        ApplyChip(ChipBorderImages, ChipTextImages, filter == FilterKind.Images);
        ApplyChip(ChipBorderColors, ChipTextColors, filter == FilterKind.Colors);
        ApplyChip(ChipBorderFiles,  ChipTextFiles,  filter == FilterKind.Files);
        ApplyChip(ChipBorderPinned, ChipTextPinned, filter == FilterKind.Pinned);
    }

    private void ApplyChip(Border border, TextBlock text, bool active)
    {
        border.Background   = active
            ? (System.Windows.Media.Brush)FindResource("ChipActiveBackground")
            : (System.Windows.Media.Brush)FindResource("ChipIdleBackground");
        border.CornerRadius = new CornerRadius(14);
        border.Padding      = new Thickness(12, 4, 12, 4);
        border.Margin       = new Thickness(0, 0, 5, 0);

        text.FontSize   = 11;
        text.FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium;
        text.Foreground = active
            ? (System.Windows.Media.Brush)FindResource("ChipActiveForeground")
            : (System.Windows.Media.Brush)FindResource("ChipIdleForeground");
    }

    private void SelectChip(FilterKind kind)
    {
        ViewModel.SelectFilter(kind);
        ApplyChipStyles();
    }

    private void ChipAll_Click(object s, WinInput.MouseButtonEventArgs e)    => SelectChip(FilterKind.All);
    private void ChipText_Click(object s, WinInput.MouseButtonEventArgs e)   => SelectChip(FilterKind.Text);
    private void ChipLinks_Click(object s, WinInput.MouseButtonEventArgs e)  => SelectChip(FilterKind.Links);
    private void ChipCode_Click(object s, WinInput.MouseButtonEventArgs e)   => SelectChip(FilterKind.Code);
    private void ChipImages_Click(object s, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Images);
    private void ChipColors_Click(object s, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Colors);
    private void ChipFiles_Click(object s, WinInput.MouseButtonEventArgs e)  => SelectChip(FilterKind.Files);
    private void ChipPinned_Click(object s, WinInput.MouseButtonEventArgs e) => SelectChip(FilterKind.Pinned);

    // ── Search ────────────────────────────────────────────────────────────────

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchClearButton_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        SearchBox.Focus();
    }

    private void FocusSearch() => SearchBox.Focus();
    private bool IsSearchFocused() => SearchBox.IsFocused;

    // ── Settings button ───────────────────────────────────────────────────────

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        App.OpenSettings();
    }

    // ── Horizontal scroll ─────────────────────────────────────────────────────

    private void CardsScrollViewer_PreviewMouseWheel(object sender, WinInput.MouseWheelEventArgs e)
    {
        CardsScrollViewer.ScrollToHorizontalOffset(
            CardsScrollViewer.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    private void CardsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        ScrollLeftBtn.Visibility  = CardsScrollViewer.HorizontalOffset > 0
            ? Visibility.Visible : Visibility.Collapsed;
        ScrollRightBtn.Visibility =
            CardsScrollViewer.HorizontalOffset < CardsScrollViewer.ScrollableWidth
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private const double CardScrollStep = 176;

    private void ScrollLeft_Click(object sender, RoutedEventArgs e)
        => CardsScrollViewer.ScrollToHorizontalOffset(
            CardsScrollViewer.HorizontalOffset - CardScrollStep);

    private void ScrollRight_Click(object sender, RoutedEventArgs e)
        => CardsScrollViewer.ScrollToHorizontalOffset(
            CardsScrollViewer.HorizontalOffset + CardScrollStep);

    // ── Scroll selected card into view ────────────────────────────────────────

    internal void ScrollSelectedIntoView()
    {
        var vm = ViewModel.SelectedCard;
        if (vm is null) return;

        var container = TryGetContainer(PinnedItemsControl, vm)
                     ?? TryGetContainer(RegularItemsControl, vm);
        if (container is null) return;

        var transform = container.TransformToAncestor(CardsPanel);
        var pos = transform.Transform(new System.Windows.Point(0, 0));

        double cardLeft  = pos.X;
        double cardRight = cardLeft + container.ActualWidth;
        double viewLeft  = CardsScrollViewer.HorizontalOffset + 20;
        double viewRight = viewLeft + CardsScrollViewer.ViewportWidth - 40;

        if (cardLeft < viewLeft)
            AnimateScroll(cardLeft - 20);
        else if (cardRight > viewRight)
            AnimateScroll(CardsScrollViewer.HorizontalOffset + (cardRight - viewRight) + 20);
    }

    private void AnimateScroll(double targetOffset)
    {
        var anim = new DoubleAnimation(
            CardsScrollViewer.HorizontalOffset,
            targetOffset,
            new Duration(TimeSpan.FromMilliseconds(150)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        CardsScrollViewer.BeginAnimation(ScrollViewerHelper.HorizontalOffsetProperty, anim);
    }

    private static FrameworkElement? TryGetContainer(ItemsControl ic, object item)
        => ic.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
}
