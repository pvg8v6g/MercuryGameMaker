using System.Windows.Input;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace GameLibrary.Behaviors;

/// <summary>
/// Sends key presses and releases to the view model as the VirtualKey that changed.
/// </summary>
public class KeyCommandBehavior
{
    #region Key Down

    public static readonly DependencyProperty KeyDownCommandProperty = DependencyProperty.RegisterAttached(
        "KeyDownCommand", typeof(ICommand), typeof(KeyCommandBehavior), new PropertyMetadata(null, OnKeyDownCommandChanged));

    public static ICommand GetKeyDownCommand(DependencyObject obj) => (ICommand) obj.GetValue(KeyDownCommandProperty);

    public static void SetKeyDownCommand(DependencyObject obj, ICommand value) => obj.SetValue(KeyDownCommandProperty, value);

    private static void OnKeyDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        element.KeyDown -= OnKeyDown;
        if (e.NewValue is ICommand) element.KeyDown += OnKeyDown;
    }

    private static void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not DependencyObject d || GetKeyDownCommand(d) is not { } command || !command.CanExecute(e.Key)) return;
        command.Execute(e.Key);
        e.Handled = true;
    }

    #endregion

    #region Key Up

    public static readonly DependencyProperty KeyUpCommandProperty = DependencyProperty.RegisterAttached(
        "KeyUpCommand", typeof(ICommand), typeof(KeyCommandBehavior), new PropertyMetadata(null, OnKeyUpCommandChanged));

    public static ICommand GetKeyUpCommand(DependencyObject obj) => (ICommand) obj.GetValue(KeyUpCommandProperty);

    public static void SetKeyUpCommand(DependencyObject obj, ICommand value) => obj.SetValue(KeyUpCommandProperty, value);

    private static void OnKeyUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        element.KeyUp -= OnKeyUp;
        if (e.NewValue is ICommand) element.KeyUp += OnKeyUp;
    }

    private static void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not DependencyObject d || GetKeyUpCommand(d) is not { } command || !command.CanExecute(e.Key)) return;
        command.Execute(e.Key);
        e.Handled = true;
    }

    #endregion

    #region Focus On Loaded

    // key events only reach a focused control, this takes focus as soon as the control is shown
    public static readonly DependencyProperty FocusOnLoadedProperty = DependencyProperty.RegisterAttached(
        "FocusOnLoaded", typeof(bool), typeof(KeyCommandBehavior), new PropertyMetadata(false, OnFocusOnLoadedChanged));

    public static bool GetFocusOnLoaded(DependencyObject obj) => (bool) obj.GetValue(FocusOnLoadedProperty);

    public static void SetFocusOnLoaded(DependencyObject obj, bool value) => obj.SetValue(FocusOnLoadedProperty, value);

    private static void OnFocusOnLoadedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Control control) return;
        control.Loaded -= OnLoaded;
        if (e.NewValue is true) control.Loaded += OnLoaded;
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Control control) control.Focus(FocusState.Programmatic);
    }

    #endregion
}
