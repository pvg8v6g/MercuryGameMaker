using GameEngine.Interface;
using Microsoft.UI.Xaml;

namespace GameEngine.UX.Views.BattleHud;

public partial class BattleHudView
{
    public static readonly DependencyProperty HudProperty = DependencyProperty.Register(
        nameof(Hud), typeof(Interface.BattleHud), typeof(BattleHudView), new PropertyMetadata(null, OnHudChanged));

    public Interface.BattleHud? Hud
    {
        get => (Interface.BattleHud?) GetValue(HudProperty);
        set => SetValue(HudProperty, value);
    }

    public BattleHudView()
    {
        InitializeComponent();
    }

    private static void OnHudChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BattleHudView view) view.Bindings.Update();
    }
}
