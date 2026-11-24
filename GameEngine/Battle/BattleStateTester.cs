using GameEngine.Interface;
using GameEngine.Models.Battle;
using GameEngine.Services.Input;
using GameLibrary.Models.States;
using GameLibrary.Services.GameData;
using Windows.System;

namespace GameEngine.Battle;

/// <summary>
/// TEST: puts any state on the fighters in a battle, to try states out before the artes that add them exist. F5 picks the
/// next state (with Shift, the one before), F6 puts it on the leader, F7 on every enemy, and F8 takes every state off
/// everyone. They always go on (no chance roll), but resistances, stacking and the boss rule count as normal.
/// </summary>
public class BattleStateTester(
    IInputService inputService,
    IGameDataService gameDataService,
    BattleStates states,
    BattleAttributes attributes,
    BattleHud battleHud)
{
    #region Fields

    private readonly HashSet<VirtualKey> _held = [];

    private int _picked = -1;

    #endregion

    #region Methods

    public void Update(ActiveBattle battle)
    {
        var next = Pressed(VirtualKey.F5);
        var onLeader = Pressed(VirtualKey.F6);
        var onEnemies = Pressed(VirtualKey.F7);
        var clear = Pressed(VirtualKey.F8);

        if (clear)
        {
            var incapacitated = states.IncapacitatedId;
            foreach (var fighter in battle.Fighters) fighter.States.RemoveAll(x => x.State.Guid != incapacitated);
            battleHud.ShowTest("Every state taken off");
            return;
        }

        if (!next && !onLeader && !onEnemies) return;

        var all = gameDataService.States.Where(x => x.Guid != states.IncapacitatedId).OrderBy(x => x.Id).ToArray();
        if (all.Length == 0) return;

        // the first press of any of them picks the first state
        if (_picked < 0 || _picked >= all.Length) _picked = 0;
        else if (next) _picked = (_picked + (inputService.IsDown(VirtualKey.Shift) ? all.Length - 1 : 1)) % all.Length;

        var state = all[_picked];
        var result = string.Empty;
        if (onLeader || onEnemies) result = Put(state, [.. battle.Fighters.Where(x => onLeader ? x.IsLeader : x.IsEnemy)]);
        battleHud.ShowTest($"{state.Name}{result}   (F5 next, F6 on leader, F7 on enemies, F8 clear)");
    }

    #endregion

    #region Private Methods

    private string Put(State state, BattleFighter[] targets)
    {
        var count = targets.Count(x => states.Add(x, state.Guid, attributes.Snapshot(x)));
        return count == 0 ? ": resisted, or already on" : $": put on {count}";
    }

    // true only on the update the key goes down
    private bool Pressed(VirtualKey key)
    {
        var down = inputService.IsDown(key);
        if (!down) _held.Remove(key);
        return down && _held.Add(key);
    }

    #endregion
}
