using Windows.System;
using GameLibrary.Enumerations;

namespace GameEngine.Services.Input;

/// <summary>
/// The keys being held. Keys are pressed on the ui thread and read on the game loop thread, so every read and write is locked.
/// The most recently pressed key is always first.
/// </summary>
public class InputService : IInputService
{
    #region Fields

    private static readonly Dictionary<VirtualKey, Direction> DirectionKeys = new()
    {
        [VirtualKey.Down] = GameLibrary.Enumerations.Direction.Down,
        [VirtualKey.Left] = GameLibrary.Enumerations.Direction.Left,
        [VirtualKey.Right] = GameLibrary.Enumerations.Direction.Right,
        [VirtualKey.Up] = GameLibrary.Enumerations.Direction.Up
    };

    private readonly object _lock = new();
    private readonly List<VirtualKey> _directions = [];
    private readonly List<VirtualKey> _keys = [];

    #endregion

    #region Properties

    public int Lag { get; set; }

    public bool Enter => KeyAny(VirtualKey.Enter);

    public bool Escape => KeyAny(VirtualKey.Escape);

    public bool Shift => KeyAny(VirtualKey.Shift);

    public bool Directional
    {
        get
        {
            lock (_lock) return _directions.Count > 0;
        }
    }

    // the most recently pressed arrow that is still held
    public Direction? Direction
    {
        get
        {
            lock (_lock) return _directions.Count > 0 ? DirectionKeys[_directions[0]] : null;
        }
    }

    public VirtualKey? Key
    {
        get
        {
            lock (_lock) return _keys.Count > 0 ? _keys[0] : null;
        }
    }

    #endregion

    #region Methods

    public void KeyPressed(VirtualKey key)
    {
        lock (_lock)
        {
            var keys = IsDirectional(key) ? _directions : _keys;
            if (!keys.Contains(key)) keys.Insert(0, key);
        }
    }

    public void KeyReleased(VirtualKey key)
    {
        lock (_lock)
        {
            if (IsDirectional(key)) _directions.Remove(key);
            else _keys.Remove(key);
        }
    }

    #endregion

    #region Private Methods

    private static bool IsDirectional(VirtualKey key) => DirectionKeys.ContainsKey(key);

    private bool KeyAny(VirtualKey key)
    {
        lock (_lock) return _keys.Contains(key);
    }

    #endregion
}
