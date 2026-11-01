using System.Collections.Concurrent;
using Windows.Foundation;
using Windows.System;
using GameEngine.Models;
using GameEngine.Services.Conditions;
using GameEngine.Services.Input;
using GameEngine.Services.Modules;
using GameEngine.Services.Movement;
using GameLibrary.Enumerations;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using MercuryLibrary.ImageComponents;

namespace GameEngine.Services.Update;

/// <summary>
/// Runs one game loop update for an entity: its active page, picture, hitboxes, movement and input.
/// Everything here runs on the game loop thread, so character sheets are measured up front when a map loads.
/// </summary>
public class UpdateService(
    IGameDataService gameDataService,
    IGraphicsService graphicsService,
    IConditionService conditionService,
    IModuleService moduleService,
    IMovementService movementService,
    IInputService inputService) : IUpdateService
{
    #region Fields

    // full image path to the image's size in pixels
    private readonly ConcurrentDictionary<string, Size> _characterSheets = [];

    #endregion

    #region Character Sheets

    // every page is measured, not just the active one, so a page change never waits on an image
    public async Task LoadCharacterSheets(IEnumerable<MapEntity> entities)
    {
        var paths = entities
            .SelectMany(x => x.Entity.Pages)
            .SelectMany(x => x.Modules)
            .OfType<CharacterImageModule>()
            .Select(CharacterPath)
            .OfType<string>()
            .Distinct()
            .Where(x => !_characterSheets.ContainsKey(x) && File.Exists(x));

        foreach (var path in paths)
        {
            var (width, height) = await graphicsService.GetImageDimensions(path);
            _characterSheets[path] = new Size(width, height);
        }
    }

    #endregion

    #region Entities

    // the highest page whose conditions all pass
    public void UpdateActivePage(MapEntity entity)
    {
        entity.ActivePage = entity.Entity.Pages
            .OrderByDescending(x => x.Id)
            .FirstOrDefault(page => page.Conditions.All(condition => conditionService.Passed(condition, page)));
    }

    public void UpdateEntity(MapEntity entity)
    {
        var characterImageModule = entity.GetModule<CharacterImageModule>();
        var positionModule = entity.GetModule<PositionModule>();
        var movementModule = entity.GetModule<MovementModule>();
        var inputModule = entity.GetModule<InputModule>();
        var hitboxModule = entity.GetModule<EntityHitboxModule>();

        if (characterImageModule is not null && positionModule is not null)
        {
            UpdateCharacterImageModule(entity, characterImageModule, positionModule);
        }

        if (positionModule is not null && hitboxModule is not null)
        {
            UpdateHitboxes(entity, positionModule, hitboxModule);
        }

        if (characterImageModule is not null && positionModule is not null && movementModule is not null)
        {
            movementService.UpdateCharacterMovement(entity, movementModule, positionModule);
        }

        if (inputModule is not null && movementModule is not null && positionModule is not null)
        {
            UpdateInput(entity, positionModule, movementModule, hitboxModule);
        }
    }

    #endregion

    #region Private Methods

    private void UpdateCharacterImageModule(MapEntity entity, CharacterImageModule characterImageModule, PositionModule positionModule)
    {
        if (CharacterPath(characterImageModule) is not { } path || !_characterSheets.TryGetValue(path, out var sheet)) return;

        // a new picture whenever the active page shows a different character
        if (entity.Picture is null || entity.PicturePath != path)
        {
            var scale = (float) gameDataService.ScaleFactor;
            entity.Picture = new PictureBox { ImagePath = path, ScaleX = scale, ScaleY = scale };
            entity.PicturePath = path;
            entity.Frame = characterImageModule.Index;
            entity.SteppingIndex = characterImageModule.SteppingIndex;
        }

        var viewport = CharacterViewport(sheet, characterImageModule.ImageName!, positionModule.Direction, entity.Frame);
        entity.Picture.Viewport = viewport;
        entity.Picture.Z = (int) (positionModule.Z + positionModule.SortZ);

        // the canvas scales a picture around its center, this keeps the scaled picture's top left on the entity
        var grow = gameDataService.ScaleFactor - 1.0;
        entity.PictureX = positionModule.X + (viewport.Width * grow / 2.0);
        entity.PictureY = positionModule.Y + (viewport.Height * grow / 2.0);
    }

    private void UpdateHitboxes(MapEntity entity, PositionModule positionModule, EntityHitboxModule hitboxModule)
    {
        var scale = gameDataService.ScaleFactor;
        foreach (var area in moduleService.GetAllHitboxes(hitboxModule, positionModule.Direction))
        {
            var hitbox = entity.GetHitbox(area);
            hitbox.OriginX = positionModule.X;
            hitbox.OriginY = positionModule.Y;
            hitbox.OffsetX = area.OffsetX * scale;
            hitbox.OffsetY = area.OffsetY * scale;
            hitbox.Width = area.Width * scale;
            hitbox.Height = area.Height * scale;
            hitbox.Z = positionModule.Z + positionModule.SortZ + 1;
        }
    }

    private void UpdateInput(MapEntity entity, PositionModule positionModule, MovementModule movementModule, EntityHitboxModule? hitboxModule)
    {
        if (inputService.Direction is not { } key) return;

        var direction = key switch
        {
            VirtualKey.Down => Direction.Down,
            VirtualKey.Left => Direction.Left,
            VirtualKey.Right => Direction.Right,
            VirtualKey.Up => Direction.Up,
            _ => throw new ArgumentException($"Invalid direction: {key}")
        };

        movementService.Move(entity, positionModule, movementModule, hitboxModule, direction);
    }

    private string? CharacterPath(CharacterImageModule characterImageModule)
    {
        return string.IsNullOrEmpty(characterImageModule.ImageName)
            ? null
            : Path.Combine(graphicsService.GetCharacterPath(), characterImageModule.ImageName);
    }

    // the frame of the sheet facing the direction, each direction is a row of frames
    private Rect CharacterViewport(Size sheet, string fileName, Direction direction, int frame)
    {
        var (columns, rows) = graphicsService.GetCharacterDivisions(fileName);
        var w = sheet.Width / columns;
        var h = sheet.Height / rows;
        var row = direction == Direction.None ? (int) Direction.Down : (int) direction;
        var bitIndex = (row * columns) + frame;
        var x = (int) (bitIndex % columns) * w;
        var y = (int) (bitIndex / columns) * h;
        return new Rect(x, y, w, h);
    }

    #endregion
}
