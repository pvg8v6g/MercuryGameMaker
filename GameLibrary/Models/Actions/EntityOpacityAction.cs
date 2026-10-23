namespace GameLibrary.Models.Actions;

public partial class EntityOpacityAction : PageAction
{
    public Guid? EntityId
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

    public override string ToString() => $"Entity Opacity: {Opacity}";
}
