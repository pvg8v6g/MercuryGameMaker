using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameEngine.Scenes;

/// <summary>
/// Builds the player: the party leader walking around the map. It is an entity like any other, the input module
/// is what the player controls, so another entity can be handed control later (puzzles, cutscenes).
/// </summary>
public class PlayerSpawner(IGameDataService gameDataService, IJsonService jsonService)
{
    #region Methods

    // the leader standing on the spawn tile (or at the exact spot, coming in from another map), null when the game has no actors yet
    public Entity? Create(SpawnPoint spawn, Point? at = null)
    {
        var party = gameDataService.GameStart.Party;
        var leader = party.Select(guid => gameDataService.Actors.FirstOrDefault(x => x.Guid == guid)).FirstOrDefault(x => x is not null)
                     ?? gameDataService.Actors.MinBy(x => x.Id);
        if (leader is null) return null;

        var player = new Entity { Name = leader.Name };
        var page = new Page { Id = 1, Name = "Player", EntityId = player.Guid };

        // the actor's picked row says which character on the sheet it is (4 to 7 on the lower half), the spawn says
        // which way it faces, the character image system splits them apart again
        var facing = spawn.Direction is Direction.None ? Direction.Down : spawn.Direction;
        var characterRow = Math.Max(0, (int) leader.CharacterDirection) / 4 * 4;

        // positions are saved unscaled like every map entity, the scene scales them when the map loads
        page.Modules.Add(new PositionModule
        {
            X = at?.X ?? spawn.TileX * gameDataService.GridSize,
            Y = at?.Y ?? spawn.TileY * gameDataService.GridSize,
            Direction = (Direction) (characterRow + (int) facing)
        });
        page.Modules.Add(new CharacterImageModule { ImageName = leader.CharacterName, Index = leader.CharacterIndex });
        page.Modules.Add(new MovementModule());
        page.Modules.Add(new InputModule());
        page.Modules.Add(new EntityHitboxModule { Collide = true, Hitboxes = Hitboxes(leader.Hitboxes) });

        player.Pages.Add(page);
        return player;
    }

    // a player without hitboxes would walk through everything, so an actor with none takes up its tile
    private Dictionary<Direction, ObservableCollection<Area>> Hitboxes(Dictionary<Direction, ObservableCollection<Area>> actorHitboxes)
    {
        if (actorHitboxes.Values.Any(x => x.Count > 0) && jsonService.Copy(actorHitboxes) is { } hitboxes) return hitboxes;

        var tile = gameDataService.GridSize;
        return new Dictionary<Direction, ObservableCollection<Area>>
        {
            [Direction.None] = [new Hitbox { OffsetX = 0, OffsetY = 0, Width = tile, Height = tile }]
        };
    }

    #endregion
}
