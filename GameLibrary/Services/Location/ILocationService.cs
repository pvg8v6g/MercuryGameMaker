namespace GameLibrary.Services.Location;

public interface ILocationService
{
    public string? GameDirectory { get; }

    public string? GraphicsDirectory { get; }

    void CreateGameDirectory();
}
