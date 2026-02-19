namespace GameLibrary.Services.Location;

public interface ILocationService
{
    string? GameDirectory { get; }

    string? GraphicsDirectory { get; }

    string? GameMakerGraphicsDirectory { get; }

    string? MercuryGameMakerDirectory { get; }

    void CreateMercuryGameDirectory();

    Task CreateGameDirectory();

    void SetGameMakerGraphicsDirectory(string path);
}
