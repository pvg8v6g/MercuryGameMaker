using Windows.System;
using GameEngine.Models.Battle;
using GameEngine.Services.Input;

namespace GameEngine.Battle.Controllers;

/// <summary>
/// The player driving the leader: an arte key going down uses that slot's arte, and an item key (Z X C V B) uses that slot's item. Walking is the arrow keys, which the
/// input system already does for the player's body (and the battle lock stops while casting or down).
/// </summary>
public class PlayerController(IInputService inputService) : IFighterController
{
    #region Constants

    // the arte keys, slot 0 first: 1-6, Q-Y, A-H
    public static readonly VirtualKey[] SlotKeys =
    [
        VirtualKey.Number1, VirtualKey.Number2, VirtualKey.Number3, VirtualKey.Number4, VirtualKey.Number5, VirtualKey.Number6,
        VirtualKey.Q, VirtualKey.W, VirtualKey.E, VirtualKey.R, VirtualKey.T, VirtualKey.Y,
        VirtualKey.A, VirtualKey.S, VirtualKey.D, VirtualKey.F, VirtualKey.G, VirtualKey.H
    ];

    // the item keys, slot 0 first
    public static readonly VirtualKey[] ItemKeys = [VirtualKey.Z, VirtualKey.X, VirtualKey.C, VirtualKey.V, VirtualKey.B];

    #endregion

    #region Fields

    private readonly HashSet<VirtualKey> _held = [];

    #endregion

    #region Methods

    public void Update(BattleFighter fighter, IBattleActions battle)
    {
        for (var i = 0; i < SlotKeys.Length; i++)
        {
            var key = SlotKeys[i];
            var down = inputService.IsDown(key);
            var pressed = down && _held.Add(key);
            if (!down) _held.Remove(key);
            if (pressed && fighter.Slots.ElementAtOrDefault(i) is { } arte) battle.Use(fighter, arte);
        }

        for (var i = 0; i < ItemKeys.Length; i++)
        {
            var key = ItemKeys[i];
            var down = inputService.IsDown(key);
            var pressed = down && _held.Add(key);
            if (!down) _held.Remove(key);
            if (pressed) battle.UseItem(fighter, i);
        }
    }

    // a new battle: nothing counts as already held
    public void Reset() => _held.Clear();

    #endregion
}
