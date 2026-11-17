using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;

namespace GameEngine.Scenes;

/// <summary>
/// Builds the player: the party leader walking around the map. It is an entity like any other, the input module
/// is what the player controls, so another entity can be handed control later (puzzles, cutscenes).
/// </summary>
public class PlayerSpawner(IGameDataService gameDataService, IGraphicsService graphicsService, IJsonService jsonService)
{
    #region Methods

    // the leader standing on the spawn tile (or at the exact spot, coming in from another map), null when the game has no actors yet
    public async Task<Entity?> Create(SpawnPoint spawn, Point? at = null)
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

        // positions are saved unscaled like every map entity, the scene scales them when the map loads.
        // A position is the sprite's top left, so the sprite is lifted until its feet stand on the spawn tile
        var (spriteX, spriteY) = await StandOnTile(leader.CharacterName, spawn.TileX, spawn.TileY);
        var feetOffset = spawn.TileY * gameDataService.GridSize - spriteY;
        page.Modules.Add(new PositionModule
        {
            X = at?.X ?? spriteX,
            Y = at?.Y ?? spriteY,
            Direction = (Direction) (characterRow + (int) facing)
        });
        page.Modules.Add(new CharacterImageModule { ImageName = leader.CharacterName, Index = leader.CharacterIndex });
        page.Modules.Add(new MovementModule());
        page.Modules.Add(new InputModule());
        page.Modules.Add(new EntityHitboxModule { Collide = true, Hitboxes = Hitboxes(leader.Hitboxes, feetOffset) });

        player.Pages.Add(page);
        return player;
    }

    // the top left that puts the bottom middle of one frame of the sheet on the bottom middle of the tile
    private async Task<(double x, double y)> StandOnTile(string? characterName, int tileX, int tileY)
    {
        var tile = gameDataService.GridSize;
        var (x, y) = ((double) tileX * tile, (double) tileY * tile);
        if (string.IsNullOrEmpty(characterName)) return (x, y);

        var path = Path.Combine(graphicsService.GetCharacterPath(), characterName);
        if (!File.Exists(path)) return (x, y);

        var (width, height) = await graphicsService.GetImageDimensions(path);
        var (columns, rows) = graphicsService.GetCharacterDivisions(characterName);
        var (frameWidth, frameHeight) = (width / columns, height / rows);
        return (x + (tile - frameWidth) / 2.0, y + tile - frameHeight);
    }

    // a player without hitboxes would walk through everything, so an actor with none takes up the tile at its feet
    // feetOffset is how far below the sprite's top the feet tile starts (48 for a 48x96 sprite)
    private Dictionary<Direction, ObservableCollection<Area>> Hitboxes(Dictionary<Direction, ObservableCollection<Area>> actorHitboxes, double feetOffset)
    {
        if (actorHitboxes.Values.Any(x => x.Count > 0) && jsonService.Copy(actorHitboxes) is { } hitboxes) return hitboxes;

        var tile = gameDataService.GridSize;
        return new Dictionary<Direction, ObservableCollection<Area>>
        {
            [Direction.None] = [new Hitbox { OffsetX = 0, OffsetY = (int) Math.Round(feetOffset), Width = tile, Height = tile }]
        };
    }

    #endregion
}
