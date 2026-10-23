namespace GameLibrary.Models.Actions;

public partial class ActorEquipmentAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? EquipmentId
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Equip
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public override string ToString() => $"Actor Equipment: {(Equip ? "Equip" : "Unequip")}";
}
