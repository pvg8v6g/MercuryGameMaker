namespace GameLibrary.Services.Location;

public class LocationService : ILocationService
{
    #region Properties

    public string? GameDirectory { get; private set; }

    public string? GraphicsDirectory { get; private set; }

    #endregion

    #region Create Game Directory

    public void CreateGameDirectory()
    {
        GameDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Tale Of Betrayal 1");
        GraphicsDirectory = Path.Combine(GameDirectory, "Graphics");
        Directory.CreateDirectory(GameDirectory);
        Directory.CreateDirectory(GraphicsDirectory);
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Animations"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Artes"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Disciplines"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Fighters"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Fighters", "Actors")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Fighters", "Enemies")));

        Directory.CreateDirectory(Path.Combine(GameDirectory, "Items"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Items", "Consumables")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Items", "Equipment")));

        Directory.CreateDirectory(Path.Combine(GameDirectory, "Maps"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Media"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "BGM")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "BGS")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "Footsteps")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "ME")));
        Directory.CreateDirectory(Path.Combine(GameDirectory, Path.Combine("Media", "SFX")));

        Directory.CreateDirectory(Path.Combine(GameDirectory, "Settings"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "States"));
        Directory.CreateDirectory(Path.Combine(GameDirectory, "Troops"));

        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Animations"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Characters"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Enemies"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Faces"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Icons"));
        Directory.CreateDirectory(Path.Combine(GraphicsDirectory, "Maps"));
    }

    #endregion
}
