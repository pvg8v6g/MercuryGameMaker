using System.Windows.Input;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace GameLibrary.Behaviors;

public static class SizeChangedBehavior
{
    public static readonly DependencyProperty SizeChangedCommandProperty = DependencyProperty.RegisterAttached(
        "SizeChangedCommand", typeof(ICommand), typeof(SizeChangedBehavior), new PropertyMetadata(null, OnSizeChangedCommandChanged));

    private static void OnSizeChangedCommandChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
    {
        if (depObj is FrameworkElement frameworkElement)
        {
            frameworkElement.SizeChanged -= OnSizeChanged; // ensure not double-subscribed
            if (e.NewValue is ICommand)
            {
                frameworkElement.SizeChanged += OnSizeChanged;
            }
        }
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement frameworkElement)
        {
            var parameter = new Size(e.NewSize.Width, e.NewSize.Height);
            if (frameworkElement.GetValue(SizeChangedCommandProperty) is ICommand command)
            {
                if (command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            }
        }
    }

    public static ICommand GetSizeChangedCommand(DependencyObject depObj)
    {
        return (ICommand)depObj.GetValue(SizeChangedCommandProperty);
    }

    public static void SetSizeChangedCommand(DependencyObject depObj, ICommand value)
    {
        depObj.SetValue(SizeChangedCommandProperty, value);
    }
}
