using System.Runtime.InteropServices;
using System.Windows;
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
        PositionAtScreenBottom();
        Opacity = 0;
        SlideTransform.Y = Height;
        Show();
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

        PositionAtScreenBottom();

        SlideTransform.Y = Height;
        Opacity = 0;
        Show();
        Activate();

        _isAnimating = true;
        var sb = new Storyboard();

        var slide = new DoubleAnimation(SlideTransform.Y, 0,
            new Duration(TimeSpan.FromMilliseconds(ShowMs)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        Storyboard.SetTarget(slide, SlideTransform);
        Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.YProperty));
        sb.Children.Add(slide);

        var fade = new DoubleAnimation(0, 1,
            new Duration(TimeSpan.FromMilliseconds(80)));
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));
        sb.Children.Add(fade);

        sb.Completed += (_, _) => _isAnimating = false;
        sb.Begin();
    }

    public void AnimatedHide(Action? onComplete = null)
    {
        if (_isAnimating) return;
        _isAnimating = true;

        var sb = new Storyboard();

        var slide = new DoubleAnimation(0, Height,
            new Duration(TimeSpan.FromMilliseconds(HideMs)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        Storyboard.SetTarget(slide, SlideTransform);
        Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.YProperty));
        sb.Children.Add(slide);

        var fade = new DoubleAnimation(1, 0,
            new Duration(TimeSpan.FromMilliseconds(60)))
        { BeginTime = TimeSpan.FromMilliseconds(120) };
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));
        sb.Children.Add(fade);

        sb.Completed += (_, _) =>
        {
            _isAnimating = false;
            Hide();
            onComplete?.Invoke();
        };
        sb.Begin();
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
            if (!IsActive && IsVisible)
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

    // Stubs — implemented in Task 6 when header/card XAML elements exist
    private void FocusSearch() { }
    private bool IsSearchFocused() => false;
    internal void ScrollSelectedIntoView() { }
}
