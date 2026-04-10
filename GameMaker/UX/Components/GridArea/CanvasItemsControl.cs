using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Components.GridArea;

public class CanvasItemsControl : ItemsControl
{
    public static readonly DependencyProperty LeftPropertyNameProperty = DependencyProperty.Register(
        nameof(LeftPropertyName), typeof(string), typeof(CanvasItemsControl), new PropertyMetadata(null));

    public string LeftPropertyName
    {
        get => (string) GetValue(LeftPropertyNameProperty);
        set => SetValue(LeftPropertyNameProperty, value);
    }

    public static readonly DependencyProperty TopPropertyNameProperty = DependencyProperty.Register(
        nameof(TopPropertyName), typeof(string), typeof(CanvasItemsControl), new PropertyMetadata(null));

    public string TopPropertyName
    {
        get => (string) GetValue(TopPropertyNameProperty);
        set => SetValue(TopPropertyNameProperty, value);
    }

    public static readonly DependencyProperty LeftOffsetProperty = DependencyProperty.Register(
        nameof(LeftOffset), typeof(double), typeof(CanvasItemsControl), new PropertyMetadata(0d, OnOffsetChanged));

    public double LeftOffset
    {
        get => (double) GetValue(LeftOffsetProperty);
        set => SetValue(LeftOffsetProperty, value);
    }

    public static readonly DependencyProperty TopOffsetProperty = DependencyProperty.Register(
        nameof(TopOffset), typeof(double), typeof(CanvasItemsControl), new PropertyMetadata(0d, OnOffsetChanged));

    public double TopOffset
    {
        get => (double) GetValue(TopOffsetProperty);
        set => SetValue(TopOffsetProperty, value);
    }

    private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CanvasItemsControl control)
            control.RefreshAllPositions();
    }

    private void RefreshAllPositions()
    {
        if (ItemsPanelRoot is not Panel panel) return;

        foreach (var child in panel.Children)
        {
            if (child is ContentPresenter { Content: { } item })
                ApplyPosition(child, item);
        }
    }

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);

        if (element is not UIElement container || item is not INotifyPropertyChanged notifier) return;

        ApplyPosition(container, item);

        notifier.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == LeftPropertyName || e.PropertyName == TopPropertyName)
                ApplyPosition(container, item);
        };
    }

    private void ApplyPosition(UIElement container, object item)
    {
        var type = item.GetType();

        if (type.GetProperty(LeftPropertyName)?.GetValue(item) is double left)
            Canvas.SetLeft(container, left + LeftOffset);
        else if (type.GetProperty(LeftPropertyName)?.GetValue(item) is int leftInt)
            Canvas.SetLeft(container, leftInt + LeftOffset);

        if (type.GetProperty(TopPropertyName)?.GetValue(item) is double top)
            Canvas.SetTop(container, top + TopOffset);
        else if (type.GetProperty(TopPropertyName)?.GetValue(item) is int topInt)
            Canvas.SetTop(container, topInt + TopOffset);
    }
}
