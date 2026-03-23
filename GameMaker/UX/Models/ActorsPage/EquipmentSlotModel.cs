using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using System.Collections.Generic;

namespace GameMaker.UX.Models.ActorsPage;

public class EquipmentSlotModel(string slotName, EquippedSlot slot, IEnumerable<Equipment> filteredEquipment)
{
    public string SlotName { get; } = slotName;

    public EquippedSlot Slot { get; } = slot;

    public IEnumerable<Equipment> FilteredEquipment { get; } = filteredEquipment;
}
