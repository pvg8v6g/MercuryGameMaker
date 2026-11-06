using GameLibrary.Configuration;

namespace GameLibrary.Services.Location;

public interface ILocationService
{
    // config.json, null until CreateGameDirectory has read it
    MercuryGameMakerConfiguration? Configuration { get; }

    string? GameDirectory { get; }

    string? GraphicsDirectory { get; }

    string? GameMakerGraphicsDirectory { get; }

    string? MercuryGameMakerDirectory { get; }

    void CreateMercuryGameDirectory();

    Task CreateGameDirectory();

    void SetGameMakerGraphicsDirectory(string path);

    void SaveConfiguration();
}
