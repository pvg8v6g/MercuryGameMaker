using GameLibrary.Enumerations;
using GameLibrary.Models.Fighter;
using GameMaker.UX.Models.MainPage;
using MercuryLibrary.WinUI3Components;
using Start = GameLibrary.Models.GameData.GameStart;

namespace GameMaker.UX.ViewModels.Popups.GameStart;

/// <summary>
/// Where a new game begins and who is in the party. The leader is the actor walking around the map.
/// </summary>
public class GameStartViewModel : PropertyChangedUpdater
{
    #region Fields

    private readonly Start _start;

    #endregion

    #region Constructor

    public GameStartViewModel(Start start, IEnumerable<Fighter> actors, string mapName)
    {
        _start = start;
        Actors = [.. actors.OrderBy(x => x.Id)];
        Members = [.. Actors.Select(x => new PartyMemberModel(x, start))];
        Location = $"{mapName}, tile {start.Spawn.TileX}, {start.Spawn.TileY}";
    }

    #endregion

    #region Properties

    public string Location { get; }

    public Fighter[] Actors { get; }

    public PartyMemberModel[] Members { get; }

    public bool HasActors => Actors.Length > 0;

    public bool HasNoActors => !HasActors;

    public Direction[] Directions { get; } = [Direction.Down, Direction.Left, Direction.Right, Direction.Up];

    public Direction Direction
    {
        get => _start.Spawn.Direction;
        set => _start.Spawn.Direction = value;
    }

    public Fighter? Leader
    {
        get => Actors.FirstOrDefault(x => x.Guid == _start.Party.FirstOrDefault());
        set
        {
            // the combo box pushes null while it loads
            if (value is null || value == Leader) return;

            _start.Party.Remove(value.Guid);
            _start.Party.Insert(0, value.Guid);
            foreach (var member in Members) member.Refresh();
            OnPropertyChanged();
        }
    }

    #endregion
}
