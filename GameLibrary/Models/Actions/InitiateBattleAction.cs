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

    public bool CanRun
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool CanLose
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Initiate Battle";
        if (!TroopName.IsNullOrEmpty()) result += $": {TroopName}";
        result += $"\n    Can Run: {(CanRun ? "Yes" : "No")}\n    Can Lose: {(CanLose ? "Yes" : "No")}";
        return result;
    }
}
