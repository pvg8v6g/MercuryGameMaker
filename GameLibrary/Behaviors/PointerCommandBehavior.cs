using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace GameLibrary.Behaviors;

public class PointerCommandBehavior
{
    public static readonly DependencyProperty CapturePointerOnPressedProperty = DependencyProperty.RegisterAttached(
        "CapturePointerOnPressed", typeof(bool), typeof(PointerCommandBehavior), new PropertyMetadata(false));

    public static readonly DependencyProperty InvokeReleasedOnPointerCaptureLostProperty = DependencyProperty.RegisterAttached(
        "InvokeReleasedOnPointerCaptureLost", typeof(bool), typeof(PointerCommandBehavior),
        new PropertyMetadata(false, OnInvokeReleasedOnPointerCaptureLostChanged));

    public static readonly DependencyProperty PointerPressedCommandProperty = DependencyProperty.RegisterAttached(
        "PointerPressedCommand", typeof(ICommand), typeof(PointerCommandBehavior), new PropertyMetadata(null, OnPointerPressedCommandChanged));

    public static readonly DependencyProperty PointerMovedCommandProperty = DependencyProperty.RegisterAttached(
        "PointerMovedCommand", typeof(ICommand), typeof(PointerCommandBehavior), new PropertyMetadata(null, OnPointerMovedCommandChanged));

    public static readonly DependencyProperty PointerReleasedCommandProperty = DependencyProperty.RegisterAttached(
        "PointerReleasedCommand", typeof(ICommand), typeof(PointerCommandBehavior), new PropertyMetadata(null, OnPointerReleasedCommandChanged));

    // gives a tab stop control keyboard focus on press so its scoped keyboard accelerators work
    public static readonly DependencyProperty FocusOnPressedProperty = DependencyProperty.RegisterAttached(
        "FocusOnPressed", typeof(bool), typeof(PointerCommandBehavior), new PropertyMetadata(false));

    public static readonly DependencyProperty PointerWheelChangedCommandProperty = DependencyProperty.RegisterAttached(
        "PointerWheelChangedCommand", typeof(ICommand), typeof(PointerCommandBehavior), new PropertyMetadata(null, OnPointerWheelChangedCommandChanged));

    // executed with the Point (relative to the element) that was double tapped
    public static readonly DependencyProperty DoubleTappedCommandProperty = DependencyProperty.RegisterAttached(
        "DoubleTappedCommand", typeof(ICommand), typeof(PointerCommandBehavior), new PropertyMetadata(null, OnDoubleTappedCommandChanged));

    public static void SetCapturePointerOnPressed(DependencyObject element, bool value)
    {
        element.SetValue(CapturePointerOnPressedProperty, value);
    }

    public static bool GetCapturePointerOnPressed(DependencyObject element)
    {
        return (bool) element.GetValue(CapturePointerOnPressedProperty);
    }

    public static void SetInvokeReleasedOnPointerCaptureLost(DependencyObject element, bool value)
    {
        element.SetValue(InvokeReleasedOnPointerCaptureLostProperty, value);
    }

    public static bool GetInvokeReleasedOnPointerCaptureLost(DependencyObject element)
    {
        return (bool) element.GetValue(InvokeReleasedOnPointerCaptureLostProperty);
    }

    public static void SetPointerPressedCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(PointerPressedCommandProperty, value);
    }

    public static ICommand? GetPointerPressedCommand(DependencyObject element)
    {
        return (ICommand?) element.GetValue(PointerPressedCommandProperty);
    }

    public static void SetPointerMovedCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(PointerMovedCommandProperty, value);
    }

    public static ICommand? GetPointerMovedCommand(DependencyObject element)
    {
        return (ICommand?) element.GetValue(PointerMovedCommandProperty);
    }

    public static void SetPointerReleasedCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(PointerReleasedCommandProperty, value);
    }

    public static ICommand? GetPointerReleasedCommand(DependencyObject element)
    {
        return (ICommand?) element.GetValue(PointerReleasedCommandProperty);
    }

    public static void SetFocusOnPressed(DependencyObject element, bool value)
    {
        element.SetValue(FocusOnPressedProperty, value);
    }

    public static bool GetFocusOnPressed(DependencyObject element)
    {
        return (bool) element.GetValue(FocusOnPressedProperty);
    }

    public static void SetPointerWheelChangedCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(PointerWheelChangedCommandProperty, value);
    }

    public static ICommand? GetPointerWheelChangedCommand(DependencyObject element)
    {
        return (ICommand?) element.GetValue(PointerWheelChangedCommandProperty);
    }

    public static void SetDoubleTappedCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(DoubleTappedCommandProperty, value);
    }

    public static ICommand? GetDoubleTappedCommand(DependencyObject element)
    {
        return (ICommand?) element.GetValue(DoubleTappedCommandProperty);
    }

    private static void OnInvokeReleasedOnPointerCaptureLostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.PointerCaptureLost -= OnPointerCaptureLost;
        if (e.NewValue is true)
        {
            element.PointerCaptureLost += OnPointerCaptureLost;
        }
    }

    private static void OnPointerPressedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.PointerPressed -= OnPointerPressed;
        if (e.NewValue is not null)
        {
            element.PointerPressed += OnPointerPressed;
        }
    }

    private static void OnPointerMovedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.PointerMoved -= OnPointerMoved;
        if (e.NewValue is not null)
        {
            element.PointerMoved += OnPointerMoved;
        }
    }

    private static void OnPointerReleasedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.PointerReleased -= OnPointerReleased;
        if (e.NewValue is not null)
        {
            element.PointerReleased += OnPointerReleased;
        }
    }

    private static void OnPointerWheelChangedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.PointerWheelChanged -= OnPointerWheelChanged;
        if (e.NewValue is not null)
        {
            element.PointerWheelChanged += OnPointerWheelChanged;
        }
    }

    private static void OnDoubleTappedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.DoubleTapped -= OnDoubleTapped;
        if (e.NewValue is not null)
        {
            element.DoubleTapped += OnDoubleTapped;
        }
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element && GetCapturePointerOnPressed(element))
        {
            element.CapturePointer(e.Pointer);
        }

        if (sender is Control control && GetFocusOnPressed(control))
        {
            control.Focus(FocusState.Pointer);
        }

        Execute(sender, e, GetPointerPressedCommand);
    }

    private static void OnPointerMoved(object sender, PointerRoutedEventArgs e) => Execute(sender, e, GetPointerMovedCommand);

    private static void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            element.ReleasePointerCapture(e.Pointer);
        }

        Execute(sender, e, GetPointerReleasedCommand);
    }

    private static void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        Execute(sender, e, GetPointerWheelChangedCommand);
        e.Handled = true;
    }

    private static void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;

        var command = GetDoubleTappedCommand(element);
        var position = e.GetPosition(element);
        if (command is null || !command.CanExecute(position)) return;

        command.Execute(position);
        e.Handled = true;
    }

    private static void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element) return;
        if (!GetInvokeReleasedOnPointerCaptureLost(element)) return;

        Execute(sender, e, GetPointerReleasedCommand);
    }

    private static void Execute(object sender, PointerRoutedEventArgs e, Func<DependencyObject, ICommand?> commandGetter)
    {
        if (sender is not UIElement element) return;

        var command = commandGetter(element);
        if (command == null) return;

        var info = new PointerEventInfo(element, e);
        if (command.CanExecute(info))
        {
            command.Execute(info);
        }
    }
}
