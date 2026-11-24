using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Components.EngineRadioIcon;

public sealed partial class EngineRadioIcon
{
    #region Registered Properties

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(EngineRadioIcon), new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?) GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty GroupNameProperty = DependencyProperty.Register(
        nameof(GroupName), typeof(string), typeof(EngineRadioIcon), new PropertyMetadata(null));

    public string? GroupName
    {
        get => (string?) GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    #endregion

    // how long the menu stays open once the pointer is on neither the icon nor the menu
    private readonly DispatcherTimer _closeTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };

    private MenuFlyoutPresenter? _menuPresenter;

    public EngineRadioIcon()
    {
        InitializeComponent();
        _closeTimer.Tick += OnCloseTimerTick;
    }

    #region Icon

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _closeTimer.Stop();
        if (Menu.IsOpen || DataContext is not EngineRadioIconModel { MenuItems.Count: > 0 } model || sender is not FrameworkElement element) return;

        Menu.Items.Clear();
        foreach (var menuItem in model.MenuItems)
        {
            Menu.Items.Add(new MenuFlyoutItem
            {
                Text = menuItem.Header,
                Command = Command,
                CommandParameter = menuItem.CommandIndex
            });
        }

        // an open menu normally takes all the pointer input outside it: the icon couldn't be clicked, and wouldn't
        // know when the pointer leaves it
        Menu.OverlayInputPassThroughElement = XamlRoot.Content;
        FlyoutBase.ShowAttachedFlyout(element);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e) => CloseSoon();

    private void OnClick(object sender, RoutedEventArgs e) => Menu.Hide();

    #endregion

    #region Menu

    private void OnMenuOpened(object? sender, object e)
    {
        if (_menuPresenter != null) return;

        DependencyObject? parent = Menu.Items.FirstOrDefault();
        while (parent is not null and not MenuFlyoutPresenter)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        if (parent is not MenuFlyoutPresenter presenter) return;

        _menuPresenter = presenter;
        presenter.AddHandler(PointerEnteredEvent, new PointerEventHandler((_, _) => _closeTimer.Stop()), true);
        presenter.AddHandler(PointerExitedEvent, new PointerEventHandler((_, _) => CloseSoon()), true);
    }

    private void OnMenuClosed(object? sender, object e) => _closeTimer.Stop();

    private void CloseSoon()
    {
        _closeTimer.Stop();
        if (Menu.IsOpen) _closeTimer.Start();
    }

    private void OnCloseTimerTick(object? sender, object e)
    {
        _closeTimer.Stop();
        Menu.Hide();
    }

    #endregion
}
