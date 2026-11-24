using GameLibrary.Models.States;

namespace GameEngine.Models.Battle;

/// <summary>
/// A state on a party member outside battle, or on its way into or out of one. It keeps what a state in a battle keeps (the
/// stacks and the clock) and the numbers of whoever applied it, taken when it was applied, for any healing or damage its tick
/// does. Whoever applied it is a party member (null when it was an enemy, which is gone once the fight is over).
/// </summary>
public class CarriedState(State state, int level, double power, double accuracy, double critical, Guid? sourceActor)
{
    public State State => state;

    public int Level => level;

    public double Power => power;

    public double Accuracy => accuracy;

    public double Critical => critical;

    public Guid? SourceActor => sourceActor;

    public int Stacks { get; set; } = 1;

    // updates left, ignored when the state is permanent (Ticks 0)
    public int Left { get; set; } = BattleState.Frames(state);

    // updates until its Tick Arte goes off next
    public int NextTick { get; set; } = Math.Max(1, state.TickInterval);

    public CarriedState Copy() => new(state, level, power, accuracy, critical, sourceActor) { Stacks = Stacks, Left = Left, NextTick = NextTick };
}
