using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Components.EngineListView;

public sealed partial class EngineListView
{
    #region Registered Properties

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(object), typeof(EngineListView), new PropertyMetadata(null));

    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(EngineListView), new PropertyMetadata(-1));

    public int SelectedIndex
    {
        get => (int) GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(EngineListView), new PropertyMetadata(null, OnItemTemplateChanged));

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?) GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EngineListView engineListView)
        {
            engineListView.UpdateItemTemplate();
        }
    }

    private void UpdateItemTemplate()
    {
        ListView.ItemTemplate = ItemTemplate ?? (DataTemplate)ListView.Resources["DefaultItemTemplate"];
    }

    public static readonly DependencyProperty NewCommandProperty = DependencyProperty.Register(
        nameof(NewCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? NewCommand
    {
        get => (ICommand?) GetValue(NewCommandProperty);
        set => SetValue(NewCommandProperty, value);
    }

    public static readonly DependencyProperty CopyCommandProperty = DependencyProperty.Register(
        nameof(CopyCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? CopyCommand
    {
        get => (ICommand?) GetValue(CopyCommandProperty);
        set => SetValue(CopyCommandProperty, value);
    }

    public static readonly DependencyProperty PasteCommandProperty = DependencyProperty.Register(
        nameof(PasteCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? PasteCommand
    {
        get => (ICommand?) GetValue(PasteCommandProperty);
        set => SetValue(PasteCommandProperty, value);
    }

    public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
        nameof(DeleteCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? DeleteCommand
    {
        get => (ICommand?) GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public static readonly DependencyProperty CopiedModelProperty = DependencyProperty.Register(
        nameof(CopiedModel), typeof(object), typeof(EngineListView), new PropertyMetadata(null));

    public object? CopiedModel
    {
        get => GetValue(CopiedModelProperty);
        set => SetValue(CopiedModelProperty, value);
    }

    #endregion

    public EngineListView()
    {
        InitializeComponent();
        UpdateItemTemplate();
    }
}
