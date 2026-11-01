using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Models.GameMaps;
using GameMaker.UX.Models.MainPage;
using MercuryLibrary.Enumerations;
using MercuryLibrary.ImageComponents;

namespace GameMaker.UX.Utilities;

/// <summary>
/// Owns the sprites a GameCanvasView draws for a map. Sprites are stored in world space and translated by the camera.
/// The canvas draws in collection order (it ignores Z), so every sprite is inserted in z order: lowest z first and,
/// on the same z, the sprite whose bottom edge is higher up the map first.
/// </summary>
public sealed class MapScene
{
    #region Fields

    private readonly Dictionary<PictureBox, SceneSprite> _sprites = [];

    #endregion

    #region Properties

    public ObservableCollection<PictureBox> Sprites { get; } = [];

    #endregion

    #region Methods

    // always Clear, never Remove: the canvas disposes the cached bitmap on Remove even when another sprite still shares it
    public void Clear()
    {
        _sprites.Clear();
        Sprites.Clear();
    }

    // full map images are optional, a map without one simply doesn't draw that layer
    public PictureBox? AddLayer(MapLayer layer, string imagePath)
    {
        if (!File.Exists(imagePath)) return null;
        return Add(imagePath, 0, 0, layer.Z, null, (float) layer.Opacity, layer.MapBlendMode);
    }

    public PictureBox Add(string imagePath, double worldX, double worldY, double z, Rect? viewport = null, float opacity = 1.0f, BlendMode? blendMode = null)
    {
        var sprite = new PictureBox
        {
            ImagePath = imagePath,
            Viewport = viewport,
            Opacity = opacity,
            BlendMode = blendMode
        };

        var scene = new SceneSprite(new Point(worldX, worldY), z, worldY + (viewport?.Height ?? 0));
        _sprites[sprite] = scene;

        // insert after everything that sorts before or level with it, so equal sprites keep the order they were added in
        var index = Sprites.Count;
        while (index > 0 && Compare(scene, _sprites[Sprites[index - 1]]) < 0) index--;
        Sprites.Insert(index, sprite);
        return sprite;
    }

    // moving does not re-sort, rebuild the scene once the move is finished
    public void Move(PictureBox sprite, double worldX, double worldY, EditorCamera camera)
    {
        if (!_sprites.TryGetValue(sprite, out var scene)) return;
        _sprites[sprite] = scene with { World = new Point(worldX, worldY) };
        Apply(sprite, camera);
    }

    public void ApplyCamera(EditorCamera camera)
    {
        foreach (var sprite in Sprites) Apply(sprite, camera);
    }

    #endregion

    #region Private Methods

    private void Apply(PictureBox sprite, EditorCamera camera)
    {
        if (!_sprites.TryGetValue(sprite, out var scene)) return;
        sprite.X = (int) Math.Round(scene.World.X - camera.X);
        sprite.Y = (int) Math.Round(scene.World.Y - camera.Y);
    }

    private static int Compare(SceneSprite a, SceneSprite b)
    {
        var z = a.Z.CompareTo(b.Z);
        return z != 0 ? z : a.Bottom.CompareTo(b.Bottom);
    }

    #endregion

    #region Types

    private sealed record SceneSprite(Point World, double Z, double Bottom);

    #endregion
}
