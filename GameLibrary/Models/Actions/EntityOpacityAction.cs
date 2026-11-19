namespace GameLibrary.Models.Actions;

public partial class EntityOpacityAction : PageAction, IEntityTargetAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    // the player (the party leader) instead of an entity
    public bool TargetsPlayer
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

    public double Opacity
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    /// <summary>Transition time in milliseconds (1000 = 1 second).</summary>
    public int TranslateTime
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => $"Entity Opacity: {Opacity}\n    Entity: {this.TargetText()}";
}
