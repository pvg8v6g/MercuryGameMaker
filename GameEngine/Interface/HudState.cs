namespace GameEngine.Interface;

/// <summary>
/// One state on a fighter on the battle HUD: its icon, how many stacks, and the same clock wipe an arte has while it cools
/// down, covering how much of its time is left. A permanent state has no wipe. Made and changed on the ui thread only.
/// </summary>
public partial class HudState(Guid id) : HudArteSlot(string.Empty)
{
    #region Properties

    public Guid Id => id;

    #endregion

    #region Methods

    // fraction is how much of its time is left, 1 just put on and 0 permanent
    public void Set(double fraction, int stacks)
    {
        Set(fraction, false, false);
        var text = stacks > 1 ? stacks.ToString() : string.Empty;
        if (text != CostText) CostText = text;
    }

    #endregion
}

// a state as the game loop read it: which, how much of its time is left (0 to 1, 0 is permanent) and its stacks
public record struct HudStateValue(Guid Id, double Fraction, int Stacks);
