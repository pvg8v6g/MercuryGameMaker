using GameLibrary.Commands;
using GameLibrary.Models.GameMaps;
using GameLibrary.Utilities.Calculations;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// Edits a map's id. The id picks the map's images (map{id}.png) so every map needs its own,
/// everything else points at maps by guid and is unaffected by a change.
/// </summary>
public partial class MapIdModel(GameMap map, IEnumerable<GameMap> maps, Func<MapIdModel, int, Task>? changed = null) : PropertyChangedUpdater
{
    #region Properties

    public GameMap Map => map;

    public string Name => map.Name;

    // bound with UpdateTrigger LostFocus so typing 12 never stops at 1 first
    public int Id
    {
        get => map.Id;
        set
        {
            if (value == map.Id) return;
            if (value < 1)
            {
                // put the box back to the real id
                Refresh();
                return;
            }

            var previous = map.Id;
            map.Id = value;
            Refresh();
            // a property setter can't wait, the change is checked for conflicts (a popup) after the id is set
            if (changed is not null) _ = changed(this, previous);
        }
    }

    public bool IsTaken => maps.Any(x => x != map && x.Id == map.Id);

    #endregion

    #region Commands

    public RelayCommand NextIdCommand => field ??= new RelayCommand(() => Id = Calculations.GetNextId([.. maps.Where(x => x != map).Select(x => x.Id)]));

    #endregion

    #region Methods

    public void Refresh()
    {
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(IsTaken));
    }

    #endregion
}
