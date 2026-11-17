using GameLibrary.Utilities;
using Windows.Foundation;
using GameLibrary.Models;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Utilities.Calculations;
using GameMaker.Services.Maps;
using GameMaker.Services.Navigation;
using GameMaker.UX.Models.MainPage;
using GameMaker.UX.Utilities;
using GameMaker.UX.ViewModels.Popups.EntityEditor;
using Page = GameLibrary.Models.Page;

namespace GameMaker.UX.ViewModels.MainPage.Tools;

/// <summary>
/// Entities mode: click an entity to select it, drag to move it tile by tile, double-click a tile to add one
/// or an entity to edit it.
/// </summary>
public class EntityTool(
    MainPageViewModel editor,
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    INavigationService navigationService,
    IMapEditService mapEditService) : IMapTool
{
    #region Fields

    private MapEntityModel? _drag;
    private Entity? _copied;

    #endregion

    #region Properties

    public int SnapSize => editor.TileSize;

    public bool CanPaste => _copied is not null;

    #endregion

    #region Input

    public void Pressed(Point world, bool leftButton)
    {
        var entity = editor.Entities.LastOrDefault(x => x.Contains(world.X, world.Y));
        editor.Select(entity);
        if (leftButton) _drag = entity;
    }

    public void Moved(Point world, bool inside)
    {
        // both are whole map pixels on the tile grid, compared as ints, so the check is exact
        var tile = Tile(world);
        if (_drag is null || ((int) _drag.X == (int) tile.X && (int) _drag.Y == (int) tile.Y)) return;

        _drag.MoveTo(tile.X, tile.Y);
        if (_drag.Picture is not { } picture || _drag.Sprite?.Rect is not { } rect) return;
        var (x, y) = MapSceneBuilder.EntitySpritePosition(_drag, rect, editor.TileSize);
        editor.Scene.Move(picture, x, y, editor.Camera);
    }

    // re-sorts so the moved entity draws in front of/behind its new neighbours
    public void Released()
    {
        if (_drag is null) return;
        _drag = null;
        editor.RebuildScene();
    }

    public string CursorText(Point world)
    {
        var tile = Tile(world);
        return $"Tile {(int) (tile.X / editor.TileSize)}, {(int) (tile.Y / editor.TileSize)}";
    }

    // a new entity on an empty tile, the editor for an entity
    public async Task DoubleTapped(Point world)
    {
        var entity = editor.Entities.LastOrDefault(x => x.Contains(world.X, world.Y));
        if (entity is null)
        {
            await NewEntity(Tile(world));
            return;
        }

        editor.Select(entity);
        await EditSelected();
    }

    #endregion

    #region Editing

    public async Task NewEntity(Point tile)
    {
        if (!editor.IsMapSelected || editor.SelectedMap is not { } map) return;

        var id = Calculations.GetNextId([.. map.Entities.Select(x => x.Id)]);
        var entity = new Entity { Id = id, Name = $"Entity {id:000}", MapId = map.Guid };
        var page = new Page { Id = 1, Name = "Page 1", EntityId = entity.Guid };
        page.Modules.Add(new PositionModule { X = tile.X, Y = tile.Y });
        entity.Pages.Add(page);
        map.Entities.Add(entity);

        var model = new MapEntityModel(entity, editor.TileSize);
        editor.Entities.Add(model);
        editor.Select(model);
        await EditSelected();
    }

    public async Task EditSelected()
    {
        if (editor.SelectedEntity is not { } model) return;

        var viewModel = new EntityEditorViewModel(model.Entity, gameDataService, jsonService, graphicsService, locationService, navigationService);
        await navigationService.ShowEntityEditorPopup(viewModel);

        model.Refresh();
        await LoadSprite(model);
        editor.RebuildScene();
    }

    public bool Delete()
    {
        if (editor.SelectedMap is not { } map || editor.SelectedEntity is not { } entity) return false;

        map.Entities.Remove(entity.Entity);
        editor.Entities.Remove(entity);
        editor.Select(null);
        editor.RebuildScene();
        return true;
    }

    public bool Copy()
    {
        if (editor.SelectedEntity is not { } entity) return false;
        _copied = entity.Entity;
        return true;
    }

    // the copy lands on the tile under the cursor
    public bool Paste(Point world)
    {
        if (!editor.IsMapSelected || editor.SelectedMap is not { } map || _copied is null) return false;
        if (jsonService.Clone(_copied) is not { } clone) return false;

        mapEditService.ReassignGuids(clone, map.Guid);
        clone.Id = Calculations.GetNextId([.. map.Entities.Select(x => x.Id)]);
        map.Entities.Add(clone);

        var tile = Tile(world);
        var model = new MapEntityModel(clone, editor.TileSize);
        model.MoveTo(tile.X, tile.Y);
        editor.Entities.Add(model);
        editor.Select(model);
        LoadSprites().Forget();
        return true;
    }

    // entities stand on tiles, so they move a tile at a time
    public void Nudge(int dx, int dy, bool big)
    {
        if (editor.SelectedEntity is not { } entity) return;

        var x = Math.Clamp(entity.X + dx * editor.TileSize, 0, Math.Max(0, editor.MapPixelWidth - editor.TileSize));
        var y = Math.Clamp(entity.Y + dy * editor.TileSize, 0, Math.Max(0, editor.MapPixelHeight - editor.TileSize));
        entity.MoveTo(x, y);
        editor.RebuildScene();
    }

    #endregion

    #region Sprites

    // the character sheet of every entity on the map, then the scene again
    public async Task LoadSprites()
    {
        var map = editor.SelectedMap;
        await Task.WhenAll(editor.Entities.ToArray().Select(LoadSprite));

        // the user may have picked another map while the sprites loaded
        if (Equals(map, editor.SelectedMap)) editor.RebuildScene();
    }

    private async Task LoadSprite(MapEntityModel entity)
    {
        if (entity.CharacterImage is not { ImageName: { } imageName } image)
        {
            entity.Sprite = null;
            return;
        }

        try
        {
            entity.Sprite = await graphicsService.GetCharacter(imageName, image.Index, entity.Direction);
        }
        catch
        {
            // a missing or broken character sheet falls back to the overlay marker
            entity.Sprite = null;
        }
    }

    #endregion

    #region Private Methods

    private Point Tile(Point world) => EditorGrid.ToCell(world, editor.TileSize, editor.MapPixelWidth, editor.MapPixelHeight);

    #endregion
}
