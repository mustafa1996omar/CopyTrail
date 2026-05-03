using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CopyTrail.ViewModels;
using WpfBrush = System.Windows.Media.Brush;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfSeparator = System.Windows.Controls.Separator;

namespace CopyTrail.Controls;

public partial class ClipCard : WpfUserControl
{
    private ClipCardViewModel? Vm => DataContext as ClipCardViewModel;

    public ClipCard()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SyncFooterState();
    }

    private void Card_Loaded(object sender, RoutedEventArgs e)
    {
        SyncFooterState();
        if (DataContext is ClipCardViewModel vm)
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ClipCardViewModel.IsSelected))
                    SyncFooterState();
            };
    }

    // ── Footer state machine ──────────────────────────────────────────────

    private enum FooterState { Default, Hover, Selected, DeleteConfirm }
    private FooterState _footerState = FooterState.Default;
    private bool _isMouseOver;

    private void SyncFooterState()
    {
        if (Vm is null) return;

        if (_footerState == FooterState.DeleteConfirm)
        {
            Show(DeleteConfirmFooter);
            Hide(DefaultFooter, HoverFooter, SelectedFooter);
            return;
        }

        if (Vm.IsSelected)
        {
            Show(SelectedFooter);
            Hide(DefaultFooter, HoverFooter, DeleteConfirmFooter);
        }
        else if (_isMouseOver)
        {
            Show(HoverFooter);
            Hide(DefaultFooter, SelectedFooter, DeleteConfirmFooter);
        }
        else
        {
            Show(DefaultFooter);
            Hide(HoverFooter, SelectedFooter, DeleteConfirmFooter);
        }
    }

    private static void Show(UIElement el) => el.Visibility = Visibility.Visible;
    private static void Hide(params UIElement[] els)
    {
        foreach (var el in els) el.Visibility = Visibility.Collapsed;
    }

    // ── Mouse hover ───────────────────────────────────────────────────────

    private void Card_MouseEnter(object sender, WpfMouseEventArgs e)
    {
        _isMouseOver = true;
        SyncFooterState();
        AnimateLift(-3);
        CardBorder.BorderBrush = (WpfBrush)FindResource("CardHoverBorder");
    }

    private void Card_MouseLeave(object sender, WpfMouseEventArgs e)
    {
        _isMouseOver = false;
        if (_footerState == FooterState.DeleteConfirm) return;
        SyncFooterState();
        AnimateLift(0);
        CardBorder.BorderBrush = Vm?.IsSelected == true
            ? (WpfBrush)FindResource("CardSelectedBorder")
            : (WpfBrush)FindResource("CardBorder");
    }

    private void AnimateLift(double to)
    {
        var anim = new DoubleAnimation(to, new Duration(TimeSpan.FromMilliseconds(120)))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        CardLiftTransform.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    // ── Click ─────────────────────────────────────────────────────────────

    private void Card_Click(object sender, WpfMouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button) return;
        if (Vm is null) return;
        GetParentPopup()?.RequestPaste(Vm);
    }

    // ── Action buttons ────────────────────────────────────────────────────

    private void PasteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        GetParentPopup()?.RequestPaste(Vm);
    }

    private void CopyBtn_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Vm is null) return;
        GetParentPopup()?.RequestCopy(Vm);
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Vm is null) return;
        GetParentPopup()?.RequestTogglePin(Vm);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ShowContextMenu();
    }

    // ── Delete ────────────────────────────────────────────────────────────

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _footerState = FooterState.DeleteConfirm;
        SyncFooterState();
    }

    private void DeleteBtn_MouseEnter(object sender, WpfMouseEventArgs e)
        => DeleteBtn.Foreground = (WpfBrush)FindResource("DeleteActionForeground");

    private void DeleteBtn_MouseLeave(object sender, WpfMouseEventArgs e)
        => DeleteBtn.Foreground = (WpfBrush)FindResource("MetaText");

    private void DeleteConfirmYes_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _footerState = FooterState.Default;
        if (Vm is null) return;
        GetParentPopup()?.RequestDelete(Vm);
    }

    private void DeleteConfirmNo_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        _footerState = FooterState.Default;
        _isMouseOver = false;
        SyncFooterState();
        AnimateLift(0);
    }

    // ── Context menu ──────────────────────────────────────────────────────

    private void ShowContextMenu()
    {
        if (Vm is null) return;

        var menu = new WpfContextMenu();
        var popup = GetParentPopup();

        menu.Items.Add(MakeMenuItem("↵ Paste", () => popup?.RequestPaste(Vm)));
        menu.Items.Add(MakeMenuItem("⎘ Copy only", () => popup?.RequestCopy(Vm)));
        menu.Items.Add(new WpfSeparator());
        menu.Items.Add(MakeMenuItem(
            Vm.IsPinned ? "Unpin" : "Pin",
            () => popup?.RequestTogglePin(Vm)));
        menu.Items.Add(new WpfSeparator());
        menu.Items.Add(MakeMenuItem("Delete", () =>
        {
            _footerState = FooterState.DeleteConfirm;
            SyncFooterState();
        }));

        menu.IsOpen = true;
    }

    private static WpfMenuItem MakeMenuItem(string header, Action action)
    {
        var item = new WpfMenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private Views.PopupWindow? GetParentPopup()
    {
        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is Views.PopupWindow pw) return pw;
            current = VisualTreeHelper.GetParent(current)
                   ?? LogicalTreeHelper.GetParent(current);
        }
        return null;
    }
}
