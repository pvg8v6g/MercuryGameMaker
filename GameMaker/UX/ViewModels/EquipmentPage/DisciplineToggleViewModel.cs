using GameLibrary.Models;
using GameLibrary.Models.Disciplines;
using GameLibrary.Models.Items;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.EquipmentPage;

/// <summary>
/// One tick box of Equippable By: a discipline that can equip the piece, or All (no discipline) which clears the others.
/// </summary>
public class DisciplineToggleViewModel(Discipline? discipline, Equipment equipment, Action changed) : PropertyChangedUpdater
{
    public string Name => discipline?.Name ?? "All";

    public GameIcon Icon => discipline?.Icon ?? GameIcon.None;

    public bool HasIcon => discipline is not null;

    public bool IsChecked
    {
        get => discipline is null ? equipment.Disciplines.Count == 0 : equipment.Disciplines.Contains(discipline.Guid);
        set
        {
            if (value == IsChecked) return;

            // All can only be turned on, which frees the piece for everyone
            if (discipline is null)
            {
                if (value) equipment.Disciplines.Clear();
            }
            else if (value)
            {
                equipment.Disciplines.Add(discipline.Guid);
            }
            else
            {
                equipment.Disciplines.Remove(discipline.Guid);
            }

            changed();
        }
    }

    public void Refresh() => OnPropertyChanged(nameof(IsChecked));
}
