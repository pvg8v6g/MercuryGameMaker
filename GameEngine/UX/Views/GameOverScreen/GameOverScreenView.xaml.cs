using GameEngine.Interface;
using Microsoft.UI.Xaml;

namespace GameEngine.UX.Views.GameOverScreen;

public partial class GameOverScreenView
{
    public static readonly DependencyProperty ScreenProperty = DependencyProperty.Register(
        nameof(Screen), typeof(Interface.GameOverScreen), typeof(GameOverScreenView), new PropertyMetadata(null, OnScreenChanged));

    public Interface.GameOverScreen? Screen
    {
        get => (Interface.GameOverScreen?) GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }

    public GameOverScreenView()
    {
        InitializeComponent();
    }

    private static void OnScreenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GameOverScreenView view) view.Bindings.Update();
    }
}
