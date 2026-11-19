using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

/// <summary>
/// Show Balloon Icon: plays a balloon from the Balloon Icons settings over the entity, once through its row.
/// </summary>
public partial class EntityBalloonIconAction : PageAction, IEntityTargetAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    // the picked entity as "001: name" for the action list, none for the page's own entity
    public string? TargetName
    {
        get;
        set => SetField(ref field, value);
    }

    // over the player (the party leader) instead of an entity
    public bool TargetsPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    // the balloon in the Balloon Icons settings, none shows nothing
    public Guid? BalloonIconId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? BalloonIconName
    {
        get;
        set => SetField(ref field, value);
    }

    // the page waits until the balloon has gone
    public bool WaitForCompletion
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = BalloonIconName.IsNullOrEmpty() ? "Show Balloon Icon" : $"Show Balloon Icon: {BalloonIconName}";
        result += $"\n    Entity: {this.TargetText()}";
        return WaitForCompletion ? result + "\n    Wait for completion" : result;
    }
}
