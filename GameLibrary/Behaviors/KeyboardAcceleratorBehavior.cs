using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace GameLibrary.Behaviors;

public class KeyboardAcceleratorBehavior
{
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(KeyboardAcceleratorBehavior),
        new PropertyMetadata(null, OnCommandChanged));

    private static void OnCommandChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
    {
        if (depObj is not KeyboardAccelerator accelerator) return;

        accelerator.Invoked -= OnInvoked;
        if (e.NewValue is ICommand) accelerator.Invoked += OnInvoked;
    }

    private static void OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        var command = GetCommand(sender);
        if (command is null || !command.CanExecute(null)) return;

        command.Execute(null);
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
}
