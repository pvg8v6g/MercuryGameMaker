using GameEngine.Battle;
using GameLibrary.Models.States;

namespace GameEngine.Models.Battle;

/// <summary>
/// One state on one fighter in a battle. It remembers who applied it (the live fighter, for who and where) and a
/// snapshot of their numbers taken when it was applied (for any damage or healing it deals).
/// </summary>
public class BattleState(State state, AttackerSnapshot attacker)
{
    public State State => state;

    public BattleFighter Source => attacker.Source;

    public AttackerSnapshot Attacker => attacker;

    public int Stacks { get; set; } = 1;

    // updates left, ignored when the state is permanent (Ticks 0)
    public int Left { get; set; } = Frames(state);

    // how long a state lasts, in updates: its Ticks are counted in Tick Intervals (Ticks 5 at 60 is 5 ticks, 5 seconds)
    public static int Frames(State state) => state.Ticks * Math.Max(1, state.TickInterval);

    // updates until its Tick Arte goes off next
    public int NextTick { get; set; } = Math.Max(1, state.TickInterval);
}
