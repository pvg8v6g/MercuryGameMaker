using GameEngine.Models;
using GameEngine.Pages;
using GameLibrary.Models.Balloons;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using MercuryLibrary.ImageComponents;
using Windows.Foundation;

namespace GameEngine.Systems;

/// <summary>
/// Plays balloon icons over entities' heads. A balloon is a row of the balloon sheet (Graphics\Balloons\~Balloons.png):
/// it starts on the first column, shows each frame for the balloon's frame time, holds the last one a moment and goes
/// away. Runs after the character images so the balloon sits on the character where it was drawn this update.
/// </summary>
public class BalloonSystem(IGameDataService gameDataService, IGraphicsService graphicsService) : IEntitySystem
{
    #region Constants

    // balloons draw over the map, its layers and everyone on it
    private const int BalloonZ = 10_000;

    // how far the balloon comes down over the top of the character, in unscaled map pixels
    private const int Overlap = 6;

    #endregion

    #region Fields

    private string? _sheetPath;
    private int _columns;

    #endregion

    #region Properties

    // the sheet, so the map can preload it with its other images. None until the sheet is made
    public string? SheetPath => _sheetPath;

    #endregion

    #region Load

    public async Task Load(IReadOnlyList<MapEntity> entities)
    {
        var path = graphicsService.GetBalloonsPath();
        var (columns, _) = await graphicsService.GetBalloonDivisions();
        _sheetPath = columns > 0 ? path : null;
        _columns = columns;
    }

    #endregion

    #region Methods

    // starts a balloon over the entity from its first frame, a balloon already playing is replaced. None or a
    // balloon that's been deleted shows nothing
    public void Show(MapEntity entity, Guid? balloonId)
    {
        var state = entity.State<BalloonState>();
        state.Balloon = gameDataService.BalloonIcons.FirstOrDefault(x => x.Guid == balloonId);
        state.Frame = 0;
        state.Timer = 0;
        if (state.Balloon is null || _sheetPath is null)
        {
            Clear(state);
            return;
        }

        var scale = (float) gameDataService.ScaleFactor;
        state.Sprite = new SceneSprite(new PictureBox { ImagePath = _sheetPath, Z = BalloonZ, ScaleX = scale, ScaleY = scale })
        {
            Width = BalloonIcon.CellSize,
            Height = BalloonIcon.CellSize
        };
    }

    public bool IsShowing(MapEntity entity) => entity.State<BalloonState>().Balloon is not null;

    #endregion

    #region Update

    public void Update(MapEntity entity)
    {
        var state = entity.State<BalloonState>();
        if (state.Balloon is not { } balloon || state.Sprite is not { } sprite) return;

        // the last frame is held for the hold time, every other frame for the frame time
        var last = state.Frame >= _columns - 1;
        var updates = Math.Max(1, gameDataService.Updates(last ? balloon.HoldTime : balloon.FrameTime));
        if (++state.Timer > updates)
        {
            state.Timer = 0;
            if (last || entity.IsErased)
            {
                Clear(state);
                return;
            }

            state.Frame++;
        }

        Place(entity, sprite, balloon, state.Frame);
    }

    #endregion

    #region Private Methods

    private static void Clear(BalloonState state)
    {
        state.Balloon = null;
        state.Sprite = null;
    }

    // centred over the character's picture, its bottom just over the top of the head
    private void Place(MapEntity entity, SceneSprite sprite, BalloonIcon balloon, int frame)
    {
        var size = BalloonIcon.CellSize;
        var scale = gameDataService.ScaleFactor;
        sprite.Picture.Viewport = new Rect(frame * size, balloon.Icon * size, size, size);

        if (entity.State<CharacterImageState>().Sprite is { } character)
        {
            sprite.X = character.X + ((character.Width - size) * scale / 2.0);
            sprite.Y = character.Y - ((size - Overlap) * scale);
            return;
        }

        // no character picture: over the tile it stands on
        if (entity.GetModule<GameLibrary.Models.Modules.PositionModule>() is not { } position) return;
        sprite.X = position.X;
        sprite.Y = position.Y - ((size - Overlap) * scale);
    }

    #endregion
}
