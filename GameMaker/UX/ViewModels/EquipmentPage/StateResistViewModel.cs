using GameLibrary.Models;
using GameLibrary.Models.States;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.EquipmentPage;

// one state's tick box in a Resist States list: equipment's, or another state's
public class StateResistViewModel(State state, List<Guid> resistStates) : PropertyChangedUpdater
{
    public string Name => state.Name;

    public GameIcon Icon => state.Icon;

    public bool IsResisted
    {
        get => resistStates.Contains(state.Guid);
        set
        {
            if (value)
            {
                if (!resistStates.Contains(state.Guid))
                    resistStates.Add(state.Guid);
            }
            else
            {
                resistStates.Remove(state.Guid);
            }
            OnPropertyChanged(nameof(IsResisted));
        }
    }
}
