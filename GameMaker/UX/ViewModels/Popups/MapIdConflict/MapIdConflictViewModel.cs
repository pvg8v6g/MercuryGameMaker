using GameMaker.UX.Models.MainPage;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.MapIdConflict;

/// <summary>
/// Two maps ended up with the same id. Both ids can be changed here until every map has its own again.
/// </summary>
public class MapIdConflictViewModel : PropertyChangedUpdater
{
    #region Constructor

    public MapIdConflictViewModel(MapIdModel first, MapIdModel second)
    {
        Maps = [first, second];

        // changing one map's id can clear or cause the other one's conflict
        foreach (var map in Maps) map.PropertyChanged += (_, _) => Refresh();
    }

    #endregion

    #region Properties

    public MapIdModel[] Maps { get; }

    public bool HasConflict => Maps.Any(x => x.IsTaken);

    public string Message => HasConflict
        ? "Every map needs its own id, it decides which map{id}.png images the map loads."
        : "All good, every map has its own id.";

    #endregion

    #region Methods

    private bool _refreshing;

    private void Refresh()
    {
        if (_refreshing) return;
        _refreshing = true;
        foreach (var map in Maps) map.Refresh();
        _refreshing = false;

        OnPropertyChanged(nameof(HasConflict));
        OnPropertyChanged(nameof(Message));
    }

    #endregion
}
