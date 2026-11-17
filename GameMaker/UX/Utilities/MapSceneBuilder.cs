using Windows.Foundation;
using GameLibrary.Models.GameMaps;
using GameMaker.UX.Models.MainPage;

namespace GameMaker.UX.Utilities;

/// <summary>
/// Fills the editor's canvas with everything on a map, drawn the way the game draws it: the map images and
/// environments at their layer z, then the characters and props sorted by where they stand.
/// </summary>
public static class MapSceneBuilder
{
    #region Methods

    public static void Build(
        MapScene scene,
        IEnumerable<MapLayerModel> layers,
        IEnumerable<MapEnvironmentModel> environments,
        IEnumerable<MapEntityModel> entities,
        IEnumerable<MapPropModel> props,
        double mapWidth,
        double mapHeight,
        int tileSize)
    {
        foreach (var layer in layers) scene.AddImage(layer.Layer, layer.ImagePath);
        foreach (var environment in environments) AddEnvironment(scene, environment, mapWidth, mapHeight);

        foreach (var entity in entities)
        {
            entity.Picture = null;
            if (entity.Sprite is not { ImagePath: { } path, Rect: { } rect }) continue;
            var (x, y) = EntitySpritePosition(entity, rect, tileSize);
            entity.Picture = scene.Add(path, x, y, entity.DrawZ, rect);
        }

        // props stand with the characters (same z) sorted by their base, flat props sit just under everyone
        foreach (var prop in props)
        {
            var z = prop.Prop.Flat ? -0.5 : 0.0;
            prop.Picture = scene.Add(prop.ImagePath, prop.X, prop.Y, z, new Rect(0, 0, prop.ImageWidth, prop.ImageHeight),
                sortBottom: prop.Prop.Flat ? null : prop.SortBottom);
        }
    }

    // sprites stand on the bottom center of their tile so tall characters overlap the tile above
    public static (double x, double y) EntitySpritePosition(MapEntityModel entity, Rect rect, int tileSize)
    {
        return (entity.X + ((tileSize - rect.Width) / 2.0), entity.Y + tileSize - rect.Height);
    }

    #endregion

    #region Private Methods

    // the first frame repeated until it covers the whole map
    private static void AddEnvironment(MapScene scene, MapEnvironmentModel environment, double mapWidth, double mapHeight)
    {
        var (width, height) = environment.FrameSize;
        if (environment.FramePaths.Length == 0 || width <= 0 || height <= 0) return;

        for (var y = 0.0; y < mapHeight; y += height)
        {
            for (var x = 0.0; x < mapWidth; x += width) scene.AddImage(environment.Environment, environment.FramePaths[0], x, y);
        }
    }

    #endregion
}
