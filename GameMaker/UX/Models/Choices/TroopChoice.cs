using GameLibrary.Models.Troops;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.Choices;

/// <summary>
/// One row of the troops a random battle can pick from: ticking it adds the troop to the list.
/// </summary>
public class TroopChoice(Troop troop, List<Guid> troopIds) : PropertyChangedUpdater
{
    #region Properties

    public string Name => troop.Name;

    public bool IsChecked
    {
        get => troopIds.Contains(troop.Guid);
        set
        {
            if (value == IsChecked) return;

            if (value) troopIds.Add(troop.Guid);
            else troopIds.Remove(troop.Guid);
            OnPropertyChanged();
        }
    }

    #endregion
}
