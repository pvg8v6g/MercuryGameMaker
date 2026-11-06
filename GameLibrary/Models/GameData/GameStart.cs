using System.Collections.ObjectModel;
using GameLibrary.Models.GameMaps;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.GameData;

/// <summary>
/// How a new game begins: the map the party starts on, where on it, and who is in the party.
/// Saved on its own in Settings\GameStart.data.
/// </summary>
public partial class GameStart : PropertyChangedUpdater
{
    #region Properties

    public Guid? MapGuid
    {
        get;
        set => SetField(ref field, value);
    }

    public SpawnPoint Spawn { get; init; } = new();

    // actor guids, the first one leads and is the one walking around the map
    public ObservableCollection<Guid> Party { get; init; } = [];

    #endregion
}
