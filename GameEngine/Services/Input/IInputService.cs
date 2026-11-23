using Windows.System;
using GameLibrary.Enumerations;

namespace GameEngine.Services.Input;

public interface IInputService
{
    int Lag { get; set; }

    bool Enter { get; }

    bool Escape { get; }

    bool Shift { get; }

    bool Directional { get; }

    Direction? Direction { get; }

    VirtualKey? Key { get; }

    // whether this key is held down right now
    bool IsDown(VirtualKey key);

    void KeyPressed(VirtualKey key);

    void KeyReleased(VirtualKey key);
}
