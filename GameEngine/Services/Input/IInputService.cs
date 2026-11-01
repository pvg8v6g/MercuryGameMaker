using Windows.System;

namespace GameEngine.Services.Input;

public interface IInputService
{
    int Lag { get; set; }

    bool Enter { get; }

    bool Escape { get; }

    bool Shift { get; }

    bool Directional { get; }

    VirtualKey? Direction { get; }

    VirtualKey? Key { get; }

    void KeyPressed(VirtualKey key);

    void KeyReleased(VirtualKey key);
}
