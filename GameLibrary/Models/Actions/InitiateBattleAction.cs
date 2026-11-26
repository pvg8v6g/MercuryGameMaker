using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class InitiateBattleAction : PageAction
{
    public Guid? TroopId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? TroopName
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>Fights one of <see cref="RandomTroopIds"/> picked at random instead of <see cref="TroopId"/>.</summary>
    public bool RandomTroop
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The troops a random battle picks from, each as likely as the others.</summary>
    public List<Guid> RandomTroopIds { get; init; } = [];

    public override string ToString()
    {
        var result = "Initiate Battle";
        if (RandomTroop) result += $": Random of {RandomTroopIds.Count} Troops";
        else if (!TroopName.IsNullOrEmpty()) result += $": {TroopName}";
        return result;
    }
}
