using GameEngine.Models;
using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// Transfer Entity: puts the entity on a tile of a map, facing a way (none keeps the way it faces). No map picked is
/// the map it's on.
/// The player changes maps the way walking off an edge does (fade out, load, fade in) and the page carries on there.
/// Another entity on the same map moves to the tile straight away. Sent to another map it's gone from this one and
/// stands on that map's tile from then on.
/// </summary>
public class EntityTransferHandler(IGameDataService gameDataService, IGameStateService gameStateService) : ActionHandler<EntityTransferAction>
{
    protected override ActionResult Start(PageRun run, EntityTransferAction action)
    {
        if (gameStateService.ActiveMap is not { } here) return ActionResult.Done;

        var map = action.MapId is { } id ? gameDataService.GameMaps.FirstOrDefault(x => x.Guid == id) ?? here : here;
        if (action.TargetsPlayer)
        {
            var facing = action.Direction is Direction.None && gameStateService.Player?.GetModule<PositionModule>() is { } playerPosition
                ? (Direction) ((int) playerPosition.Direction % 4)
                : action.Direction;
            gameStateService.EdgeTransfer = new EdgeTransfer(map, action.X, action.Y, facing, OnTile: true);
            run.CarriesOver = true;
            return ActionResult.Busy;
        }

        if (gameStateService.Find(run, action.EntityId) is not { } entity) return ActionResult.Done;

        if (map.Guid == here.Guid) Place(entity, action);
        else Send(entity, here, map, action);
        return ActionResult.Done;
    }

    // the player's transfer holds the page until the new map is up
    protected override ActionResult Update(PageRun run, EntityTransferAction action)
    {
        return gameStateService.EdgeTransfer is not null || gameStateService.IsTransferring ? ActionResult.Busy : ActionResult.Done;
    }

    // the bottom middle of its frame on the bottom middle of the tile, like the player's spawn
    private void Place(MapEntity entity, EntityTransferAction action)
    {
        if (entity.GetModule<PositionModule>() is not { } position) return;

        var tile = gameDataService.GridSize;
        var scale = gameDataService.ScaleFactor;
        var frame = entity.State<CharacterImageState>().Sprite is { } sprite ? (sprite.Width, sprite.Height) : (tile, tile);
        position.X = ((action.X * tile) + ((tile - frame.Item1) / 2.0)) * scale;
        position.Y = ((action.Y * tile) + tile - frame.Item2) * scale;
        if (action.Direction is not Direction.None) position.Direction = action.Direction;

        // a walk or route it was on started somewhere else
        entity.State<RouteState>().Walk.Reset();
    }

    // a visitor entry is a page putting the entity on that map's tile, its own map included: the map shows that
    // copy instead of the one from the editor
    private void Send(MapEntity entity, GameMap here, GameMap there, EntityTransferAction action)
    {
        var guid = entity.Entity.Guid;

        // as it was made in the editor: its own map's saved entity, or the copy it was sent here with
        var saved = gameStateService.VisitingEntities.GetValueOrDefault(here.Guid)?.FirstOrDefault(x => x.Entity.Guid == guid)?.Entity
                    ?? here.Entities.FirstOrDefault(x => x.Guid == guid);
        if (saved is null) return;

        foreach (var visitors in gameStateService.VisitingEntities.Values) visitors.RemoveAll(x => x.Entity.Guid == guid);
        if (here.Entities.Any(x => x.Guid == guid)) gameStateService.DepartedEntities.Add((here.Guid, guid));
        gameStateService.DepartedEntities.Remove((there.Guid, guid));

        if (!gameStateService.VisitingEntities.TryGetValue(there.Guid, out var arriving)) gameStateService.VisitingEntities[there.Guid] = arriving = [];
        arriving.Add(new VisitingEntity(saved, action.X, action.Y, action.Direction));

        // it stands on the tile it was sent to, not where it last stood on either map
        gameStateService.EntityPlaces.Remove((there.Guid, guid));
        gameStateService.EntityPlaces.Remove((here.Guid, guid));
        entity.IsErased = true;
    }
}
