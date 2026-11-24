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

    public override string ToString()
    {
        var result = "Initiate Battle";
        if (!TroopName.IsNullOrEmpty()) result += $": {TroopName}";
        return result;
    }
}
