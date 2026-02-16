namespace GameLibrary.Services.Location;

public interface ILocationService
{
    public string? GameDirectory { get; }

    public string? GraphicsDirectory { get; }

    public string? GameMakerGraphicsDirectory { get; }

    void CreateMercuryGameDirectory();

    Task CreateGameDirectory();

    void SetGameMakerGraphicsDirectory(string path);
}
