namespace GameLibrary.Models.Actions;

public partial class ScrollMapAction : PageAction
{
    /// <summary>When set the camera scrolls to the player instead of an entity or location.</summary>
    public bool TargetPlayer
    {
        get;
        set => SetField(ref field, value);
    }

    /// <summary>The entity to scroll the camera to. When null (and not targeting the player) the location is used.</summary>
    public Guid? TargetEntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public int TargetX
    {
        get;
        set => SetField(ref field, value);
    }

    public int TargetY
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        if (TargetPlayer) return "Scroll Map To Player";
        if (TargetEntityId != null) return "Scroll Map To Entity";
        return $"Scroll Map To Location: {TargetX}, {TargetY}";
    }
}
