using System.Collections.Concurrent;
using Windows.Foundation;
using GameEngine.Models;
using GameEngine.Services.GameState;
using GameLibrary.Enumerations;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using MercuryLibrary.ImageComponents;

namespace GameEngine.Systems;

/// <summary>
/// Draws each entity's character sheet frame where the entity stands and animates the walk.
/// Sheets are measured while the map loads so an update never waits on an image.
/// </summary>
public class CharacterImageSystem(IGameDataService gameDataService, IGameStateService gameStateService, IGraphicsService graphicsService) : IEntitySystem
{
    #region Properties

    // a paused map still shows its characters, they just stop walking
    public bool RunsWhilePaused => true;

    #endregion

    #region Constants

    private const int StandingFrame = 1;

    // how many updates each step frame is shown for
    private const int StepUpdates = 15;

    // how many updates of standing before the feet start over
    private const int IdleUpdates = 90;

    #endregion

    #region Fields

    // full image path to the sheet's size in pixels
    private readonly ConcurrentDictionary<string, Size> _sheets = [];

    #endregion

    #region Load

    // every page is measured, not just the active one, so a page change never waits on an image
    public async Task Load(IReadOnlyList<MapEntity> entities)
    {
        var paths = entities
            .SelectMany(x => x.Entity.Pages)
            .SelectMany(x => x.Modules)
            .OfType<CharacterImageModule>()
            .Select(SheetPath)
            .OfType<string>()
            .Distinct()
            .Where(x => !_sheets.ContainsKey(x) && File.Exists(x));

        foreach (var path in paths)
        {
            var (width, height) = await graphicsService.GetImageDimensions(path);
            _sheets[path] = new Size(width, height);
        }
    }

    #endregion

    #region Update

    public void Update(MapEntity entity)
    {
        if (entity.GetModule<CharacterImageModule>() is not { } image || entity.GetModule<PositionModule>() is not { } position) return;
        if (SheetPath(image) is not { } path || !_sheets.TryGetValue(path, out var sheet)) return;

        var state = entity.State<CharacterImageState>();
        var sprite = state.ImagePath == path && state.Index == image.Index && state.Sprite is not null
            ? state.Sprite
            : NewSprite(state, image, path);

        if (state.Walks)
        {
            FindCharacter(state, position);
            if (!gameStateService.IsTransferring && entity.GetModule<MovementModule>() is { } movementModule) Animate(state, movementModule, entity.State<MovementState>());
        }

        var viewport = Viewport(sheet, image.ImageName!, state, position.Direction);
        sprite.Picture.Viewport = viewport;
        sprite.Picture.Z = (int) (position.Z + position.SortZ);
        sprite.X = position.X;
        sprite.Y = position.Y;
        sprite.Width = viewport.Width;
        sprite.Height = viewport.Height;
        sprite.SortY = position.Y + (viewport.Height * gameDataService.ScaleFactor);
    }

    #endregion

    #region Private Methods

    // a new sprite whenever the active page shows a different character
    private SceneSprite NewSprite(CharacterImageState state, CharacterImageModule image, string path)
    {
        var scale = (float) gameDataService.ScaleFactor;
        state.Sprite = new SceneSprite(new PictureBox { ImagePath = path, ScaleX = scale, ScaleY = scale });
        state.ImagePath = path;
        state.Index = image.Index;

        // only sheets made of 3 by 4 characters can walk, anything else shows the picked cell as it is
        var (columns, rows) = graphicsService.GetCharacterDivisions(image.ImageName!);
        state.Walks = (int) columns % 3 == 0 && (int) rows % 4 == 0;
        state.Column = state.Walks ? image.Index / 3 * 3 : image.Index;
        state.Row = 0;
        state.Frame = 1;
        state.SteppingIndex = 1;
        return state.Sprite;
    }

    // the editor saves the picked row as the direction (4 to 7 on the lower half of a sheet), that row tells the
    // character apart and the direction is what is left over, so turning keeps the same character
    private static void FindCharacter(CharacterImageState state, PositionModule position)
    {
        if ((int) position.Direction < 4) return;

        state.Row = (int) position.Direction / 4 * 4;
        position.Direction = (Direction) ((int) position.Direction % 4);
    }

    // standing shows the middle frame, walking alternates the outer frames (the feet)
    private static void Animate(CharacterImageState state, MovementModule movementModule, MovementState movement)
    {
        if (!movementModule.Stepping) return;

        if (movement.Stepped)
        {
            state.NotMovingTimer = 0;

            // the first step shows straight away so even a tap of the key takes a step
            if (state.Frame == StandingFrame || ++state.SteppingTimer >= StepUpdates) TakeStep(state);
            return;
        }

        // a step that was just taken finishes showing before the character stands
        if (state.Frame != StandingFrame && ++state.SteppingTimer < StepUpdates) return;

        state.Frame = StandingFrame;
        state.SteppingTimer = 0;

        // after standing a while the next walk starts on the first foot again
        if (++state.NotMovingTimer <= IdleUpdates) return;
        state.SteppingIndex = StandingFrame;
        state.NotMovingTimer = 0;
    }

    // swaps to the other foot, so tapping the key walks left foot, right foot instead of the same foot every time
    private static void TakeStep(CharacterImageState state)
    {
        state.SteppingIndex = state.SteppingIndex == 0 ? 2 : 0;
        state.Frame = state.SteppingIndex;
        state.SteppingTimer = 0;
    }

    private string? SheetPath(CharacterImageModule image)
    {
        return string.IsNullOrEmpty(image.ImageName) ? null : Path.Combine(graphicsService.GetCharacterPath(), image.ImageName);
    }

    // the cell of the sheet showing the character facing the direction, each direction is a row of frames
    private Rect Viewport(Size sheet, string fileName, CharacterImageState state, Direction direction)
    {
        var (columns, rows) = graphicsService.GetCharacterDivisions(fileName);
        var w = sheet.Width / columns;
        var h = sheet.Height / rows;
        var facing = direction == Direction.None ? (int) Direction.Down : (int) direction;
        var column = state.Walks ? state.Column + state.Frame : state.Column;
        var row = state.Row + facing;
        return new Rect(column * w, row * h, w, h);
    }

    #endregion
}
