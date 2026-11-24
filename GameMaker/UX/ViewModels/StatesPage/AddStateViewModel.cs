using GameLibrary.Models.States;
using GameLibrary.Models;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.StatesPage;

/// <summary>
/// One row of the states an arte or a consumable adds: whether it adds the state, and the percent chance of it going on.
/// </summary>
public class AddStateViewModel(State state, List<Guid> addStates, Dictionary<Guid, int> chances) : PropertyChangedUpdater
{
    #region Constants

    private const int Always = 100;

    #endregion

    #region Properties

    public string Name => state.Name;

    public GameIcon Icon => state.Icon;

    public bool IsChecked
    {
        get => addStates.Contains(state.Guid);
        set
        {
            if (value == IsChecked) return;

            if (value)
            {
                addStates.Add(state.Guid);
            }
            else
            {
                addStates.Remove(state.Guid);
                chances.Remove(state.Guid);
                OnPropertyChanged(nameof(Chance));
            }

            OnPropertyChanged();
        }
    }

    // only a chance under 100 is kept. An emptied box changes nothing, so a new number can be typed
    public string Chance
    {
        get => chances.GetValueOrDefault(state.Guid, Always).ToString();
        set
        {
            if (!IsChecked || !int.TryParse(value, out var result)) return;

            result = Math.Clamp(result, 0, Always);
            if (result == Always) chances.Remove(state.Guid);
            else chances[state.Guid] = result;

            OnPropertyChanged();
        }
    }

    #endregion
}
