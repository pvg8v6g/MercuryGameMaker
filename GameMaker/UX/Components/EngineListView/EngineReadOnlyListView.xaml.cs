using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.EngineListView;

public sealed partial class EngineReadOnlyListView
{
    #region Registered Properties

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(object), typeof(EngineReadOnlyListView), new PropertyMetadata(null));

    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(EngineReadOnlyListView), new PropertyMetadata(-1));

    public int SelectedIndex
    {
        get => (int) GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public static readonly DependencyProperty ListViewHeightProperty = DependencyProperty.Register(
        nameof(ListViewHeight), typeof(double), typeof(EngineReadOnlyListView), new PropertyMetadata(double.NaN));

    public double ListViewHeight
    {
        get => (double) GetValue(ListViewHeightProperty);
        set => SetValue(ListViewHeightProperty, value);
    }

    #endregion

    public EngineReadOnlyListView()
    {
        InitializeComponent();
    }
}
