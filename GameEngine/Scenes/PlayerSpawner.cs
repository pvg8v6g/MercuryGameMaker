using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameEngine.Models;
using GameEngine.Services.Party;
using GameEngine.Systems;

namespace GameEngine.Scenes;

/// <summary>
/// Builds the player: the party leader walking around the map. It is an entity like any other, the input module
/// is what the player controls, so another entity can be handed control later (puzzles, cutscenes).
/// </summary>
public class PlayerSpawner(
    IGameDataService gameDataService,
    IGraphicsService graphicsService,
    IJsonService jsonService,
    PartyService partyService,
    CharacterImageSystem characterImageSystem)
{
    #region Methods

    // the leader standing on the spawn tile (or at the exact spot, coming in from another map), null when the game has no actors yet
    public async Task<Entity?> Create(SpawnPoint spawn, Point? at = null)
    {
        // the party's first actor as it is in this game (pages may have changed its look), the first actor made
        // when the party is empty
        var leader = partyService.Leader ?? partyService.Actor(gameDataService.Actors.MinBy(x => x.Id)?.Guid);
        if (leader is null) return null;

        // positions are saved unscaled like every map entity, the scene scales them when the map loads.
        // A position is the sprite's top left, so the sprite is lifted until its feet stand on the spawn tile
        var (spriteX, spriteY) = await StandOnTile(leader.CharacterName, spawn.TileX, spawn.TileY);
        var facing = spawn.Direction is Direction.None ? Direction.Down : spawn.Direction;
        var player = await CreateFighter(leader, at?.X ?? spriteX, at?.Y ?? spriteY, facing, "Player");
        player.Pages[0].Modules.Add(new InputModule());
        return player;
    }

    // a fighter's body on the map: its sprite with its top left at x, y (unscaled), facing a way, walking and solid.
    // The player is one of these with an input module, battles use them for the rest of the party and the enemies
    public async Task<Entity> CreateFighter(Fighter fighter, double x, double y, Direction facing, string pageName)
    {
        var entity = new Entity { Name = fighter.Name };
        var page = new Page { Id = 1, Name = pageName, EntityId = entity.Guid };

        // the fighter's picked row says which character on the sheet it is (4 to 7 on the lower half), facing says
        // which way it looks, the character image system splits them apart again
        var characterRow = Math.Max(0, (int) fighter.CharacterDirection) / 4 * 4;
        var (_, frameHeight) = await FrameSize(fighter.CharacterName);
        page.Modules.Add(new PositionModule { X = x, Y = y, Direction = (Direction) (characterRow + (int) facing) });
        page.Modules.Add(new CharacterImageModule { ImageName = fighter.CharacterName, Index = fighter.CharacterIndex });
        page.Modules.Add(new MovementModule());
        page.Modules.Add(new EntityHitboxModule { Collide = true, Hitboxes = Hitboxes(fighter.Hitboxes, frameHeight - gameDataService.GridSize) });

        entity.Pages.Add(page);
        return entity;
    }

    // the top left that puts the bottom middle of one frame on the bottom middle of the tile whose top left is x, y
    // (unscaled pixels), for spots that aren't on whole tiles like a troop's spawn points
    public async Task<(double x, double y)> StandAt(string? characterName, double x, double y)
    {
        var tile = gameDataService.GridSize;
        var (frameWidth, frameHeight) = await FrameSize(characterName);
        return (x + (tile - frameWidth) / 2.0, y + tile - frameHeight);
    }

    // one frame of the character's sheet, a tile when it has no sheet
    private async Task<(double width, double height)> FrameSize(string? characterName)
    {
        var tile = gameDataService.GridSize;
        if (string.IsNullOrEmpty(characterName)) return (tile, tile);

        var path = Path.Combine(graphicsService.GetCharacterPath(), characterName);
        if (!File.Exists(path)) return (tile, tile);

        var (width, height) = await graphicsService.GetImageDimensions(path);
        var (columns, rows) = graphicsService.GetCharacterDivisions(characterName);
        return (width / columns, height / rows);
    }

    // the leader changed (the party or the leader's look): the player becomes the new leader where it stands
    public void Refresh(MapEntity? player)
    {
        if (player?.GetModule<CharacterImageModule>() is not { } image || player.GetModule<PositionModule>() is not { } position) return;
        if (partyService.Leader is not { } leader) return;

        player.Entity.Name = leader.Name;
        image.ImageName = leader.CharacterName;
        image.Index = leader.CharacterIndex;

        // the row picks the character on the sheet like at the spawn, the way it faces stays
        var characterRow = Math.Max(0, (int) leader.CharacterDirection) / 4 * 4;
        position.Direction = (Direction) (characterRow + (Math.Max(0, (int) position.Direction) % 4));
        _ = characterImageSystem.Measure(leader.CharacterName);
    }

    // the top left that puts the bottom middle of one frame of the sheet on the bottom middle of the tile
    public Task<(double x, double y)> StandOnTile(string? characterName, int tileX, int tileY)
    {
        var tile = gameDataService.GridSize;
        return StandAt(characterName, (double) tileX * tile, (double) tileY * tile);
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
