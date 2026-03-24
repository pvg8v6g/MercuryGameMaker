using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.ActorsPage;

public class EquipmentSlotModel : PropertyChangedUpdater
{
    public EquipmentSlotModel(string slotName, EquippedSlot slot, Equipment[] filteredEquipment)
    {
        SlotName = slotName;
        Slot = slot;
        Options = filteredEquipment
            .Select(e => new EquipmentOption(e)).Prepend(new EquipmentOption(null))
            .ToArray();

        Slot.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Slot.Id))
            {
                OnPropertyChanged(nameof(SelectedIndex));
            }
        };
    }

    public string SlotName { get; }

    public string ToolTipText => $"Starting {SlotName} equipment for this actor.";

    public EquippedSlot Slot { get; }

    public EquipmentOption[] Options { get; }

    public int SelectedIndex
    {
        get => Slot.Id == null ? -1 : Array.FindIndex(Options, o => o.Guid == Slot.Id);
        set
        {
            if (value >= 0 && value < Options.Length)
            {
                var guid = Options[value].Guid;
                Slot.Id = guid == Guid.Empty ? null : guid;
            }
            else
            {
                Slot.Id = null;
            }

            OnPropertyChanged();
        }
    }
}
