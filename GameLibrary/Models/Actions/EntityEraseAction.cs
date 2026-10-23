namespace GameLibrary.Models.Actions;

public partial class EntityEraseAction : PageAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() => "Erase Entity";
}
