using GameLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Fighter;

public partial class EquippedSlot : PropertyChangedUpdater
{
    public EquipmentLocation Location
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? Id
    {
        get;
        set => SetField(ref field, value);
    }
}
