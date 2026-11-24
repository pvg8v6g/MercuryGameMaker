using GameEngine.Interface;
using Microsoft.UI.Xaml;

namespace GameEngine.UX.Views.VictoryScreen;

public partial class VictoryScreenView
{
    public static readonly DependencyProperty ScreenProperty = DependencyProperty.Register(
        nameof(Screen), typeof(Interface.VictoryScreen), typeof(VictoryScreenView), new PropertyMetadata(null, OnScreenChanged));

    public Interface.VictoryScreen? Screen
    {
        get => (Interface.VictoryScreen?) GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }

    public VictoryScreenView()
    {
        InitializeComponent();
    }

    private static void OnScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VictoryScreenView view) view.Bindings.Update();
    }
}
