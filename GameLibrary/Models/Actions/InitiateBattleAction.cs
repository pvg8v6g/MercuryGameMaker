namespace GameLibrary.Models.Actions;

public partial class InitiateBattleAction : PageAction
{
    public Guid? TroopId
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

    public override string ToString() => "Initiate Battle";
}
