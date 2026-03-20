using GameLibrary.Enumerations;

namespace GameLibrary.Models.Items;

public partial class Equipment : Item
{
    #region Properties

    public EquipmentLocation EquipmentLocation
    {
        get;
        set => SetField(ref field, value);
    } = EquipmentLocation.Head;

    public bool ResistAllStates
    {
        get;
        set => SetField(ref field, value);
    }

    public Dictionary<Guid, int> AttributeStats { get; init; } = new();

    public Dictionary<Guid, int> ElementResist { get; init; } = new();

    public List<Guid> ResistStates { get; init; } = [];

    #endregion
}
