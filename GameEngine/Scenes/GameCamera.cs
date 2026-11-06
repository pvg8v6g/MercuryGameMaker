using Windows.Foundation;
using GameEngine.Models;

namespace GameEngine.Scenes;

/// <summary>
/// The part of the map on screen. X/Y is the map pixel at the top left of the view.
/// The camera centers on an entity it follows or on a point, and can slide to either instead of jumping,
/// like showing a door that just opened and then sliding back to the player. It never shows past the map edge.
/// </summary>
public class GameCamera
{
    #region Fields

    private double _speed;

    #endregion

    #region Properties

    public double X { get; private set; }

    public double Y { get; private set; }

    // the entity being followed, the scene starts on the player
    public MapEntity? Target { get; private set; }

    // the map point the camera is held on instead of an entity
    public Point? Point { get; private set; }

    public bool IsSliding { get; private set; }

    #endregion

    #region Methods

    // jumps to the entity and follows it
    public void Follow(MapEntity? target)
    {
        Target = target;
        Point = null;
        IsSliding = false;
    }

    // slides until centered on the map point and stays there
    public void SlideTo(Point point, double speed)
    {
        Point = point;
        _speed = speed;
        IsSliding = true;
    }

    // slides until centered on the entity and then follows it
    public void SlideTo(MapEntity target, double speed)
    {
        Target = target;
        Point = null;
        _speed = speed;
        IsSliding = true;
    }

    // moves toward the center (a map point) for this update
    public void Update(Point center, Size view, Size map)
    {
        var goalX = Math.Clamp(center.X - (view.Width / 2.0), 0, Math.Max(0, map.Width - view.Width));
        var goalY = Math.Clamp(center.Y - (view.Height / 2.0), 0, Math.Max(0, map.Height - view.Height));

        var dx = goalX - X;
        var dy = goalY - Y;
        var distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (!IsSliding || distance <= _speed)
        {
            X = goalX;
            Y = goalY;
            IsSliding = false;
            return;
        }

        X += dx / distance * _speed;
        Y += dy / distance * _speed;
    }

    #endregion
}
