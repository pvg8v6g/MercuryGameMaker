using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace GameLibrary.Behaviors;

public class KeyboardAcceleratorBehavior
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(KeyboardAcceleratorBehavior),
        new PropertyMetadata(null, OnCommandChanged));

    // passed to the command, e.g. which way an arrow key nudges
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
        "CommandParameter", typeof(object), typeof(KeyboardAcceleratorBehavior), new PropertyMetadata(null));

    private static void OnCommandChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
    {
        if (depObj is not KeyboardAccelerator accelerator) return;

        accelerator.Invoked -= OnInvoked;
        if (e.NewValue is ICommand) accelerator.Invoked += OnInvoked;
    }

    private static void OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var command = GetCommand(sender);
        var parameter = GetCommandParameter(sender);
        if (command is null || !command.CanExecute(parameter)) return;

        command.Execute(parameter);
        args.Handled = true;
    }

    public static ICommand GetCommand(DependencyObject depObj)
    {
        return (ICommand) depObj.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject depObj, ICommand value)
    {
        depObj.SetValue(CommandProperty, value);
    }

    public static object? GetCommandParameter(DependencyObject depObj)
    {
        return depObj.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(DependencyObject depObj, object? value)
    {
        depObj.SetValue(CommandParameterProperty, value);
    }
}
