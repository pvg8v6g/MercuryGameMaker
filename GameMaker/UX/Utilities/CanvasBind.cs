using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Utilities;

public static class CanvasBind
{
    public static readonly DependencyProperty LeftProperty = DependencyProperty.RegisterAttached(
        "Left",
        typeof(double),
        typeof(CanvasBind),
        new PropertyMetadata(0d, OnLeftChanged));

    public static void SetLeft(DependencyObject element, double value)
    {
        element.SetValue(LeftProperty, value);
    }

    public static double GetLeft(DependencyObject element)
    {
        return (double) element.GetValue(LeftProperty);
    }

    private static void OnLeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            Canvas.SetLeft(element, (double) e.NewValue);
        }
    }

    public static readonly DependencyProperty TopProperty = DependencyProperty.RegisterAttached(
        "Top",
        typeof(double),
        typeof(CanvasBind),
        new PropertyMetadata(0d, OnTopChanged));

    public static void SetTop(DependencyObject element, double value)
    {
        element.SetValue(TopProperty, value);
    }

    public static double GetTop(DependencyObject element)
    {
        return (double) element.GetValue(TopProperty);
    }

    private static void OnTopChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            Canvas.SetTop(element, (double) e.NewValue);
        }
    }
}
