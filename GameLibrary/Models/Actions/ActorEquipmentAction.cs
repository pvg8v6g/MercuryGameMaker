using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorEquipmentAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ActorName
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? EquipmentId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? EquipmentName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Equip
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public override string ToString()
    {
        var result = $"Actor Equipment: {(Equip ? "Equip" : "Unequip")}";
        if (!ActorName.IsNullOrEmpty()) result += $"\n    Actor: {ActorName}";
        if (!EquipmentName.IsNullOrEmpty()) result += $"\n    Equipment: {EquipmentName}";
        return result;
    }
}
