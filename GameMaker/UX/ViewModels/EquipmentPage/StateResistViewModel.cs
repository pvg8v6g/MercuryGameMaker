using GameLibrary.Models.Items;
using GameLibrary.Models.States;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.EquipmentPage;

public class StateResistViewModel(State state, Equipment equipment) : PropertyChangedUpdater
{
    public string Name => state.Name;

    public int Icon => state.Icon;

    public bool IsResisted
    {
        get => equipment.ResistStates.Contains(state.Guid);
        set
        {
            if (value)
            {
                if (!equipment.ResistStates.Contains(state.Guid))
                    equipment.ResistStates.Add(state.Guid);
            }
            else
            {
                equipment.ResistStates.Remove(state.Guid);
            }
            OnPropertyChanged(nameof(IsResisted));
        }
    }
}
