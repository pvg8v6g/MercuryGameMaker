using GameLibrary.Models;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Tasks;

namespace GameMaker.Tasks;

/// <summary>
/// Saves the game without ever losing data:
/// 1. every file is built in memory first, anything that can't be saved stops the save before the disk is touched
/// 2. the last save is copied to a backup (Documents\Mercury Game Maker\Backups), the newest few are kept
/// 3. each file is written next to the old one and swapped in, so a file is always either the old or the new one
/// 4. only once every file is written are files of deleted data removed
/// </summary>
public class SaveDataTask(ILocationService locationService, IGameDataService gameDataService, IJsonService jsonService) : EngineTask
{
    #region Constants

    private const string DataExtension = ".data";
    private const string TempExtension = ".saving";
    private const int BackupsKept = 10;

    // every folder of saved data, relative to the game folder
    private static readonly string[] DataFolders =
    [
        Path.Combine("Settings", "Attributes"),
        Path.Combine("Settings", "Elements"),
        Path.Combine("Settings", "Growths"),
        Path.Combine("Fighters", "Actors"),
        "Disciplines",
        "Artes",
        Path.Combine("Items", "Consumables"),
        Path.Combine("Items", "Equipment"),
        Path.Combine("Fighters", "Enemies"),
        "Troops",
        "States",
        "Animations",
        "Maps"
    ];

    private static readonly string GameStartFile = Path.Combine("Settings", "GameStart.data");

    #endregion

    #region Call

    public override Task Call()
    {
        var gameDirectory = locationService.GameDirectory!;
        var files = BuildFiles(gameDirectory);

        // a file for every write, one for the backup, one for the clean up
        MaxWork = files.Count + 2;

        Backup(gameDirectory);
        Work++;

        foreach (var (path, json) in files)
        {
            WriteFile(path, json);
            Work++;
        }

        RemoveDeletedData(gameDirectory, files.Keys);
        Work = MaxWork;
        return Task.CompletedTask;
    }

    #endregion

    #region Build

    // every file the save writes, full path to its text
    private Dictionary<string, string> BuildFiles(string gameDirectory)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            Add(files, gameDirectory, DataFolders[0], gameDataService.Attributes);
            Add(files, gameDirectory, DataFolders[1], gameDataService.Elements);
            Add(files, gameDirectory, DataFolders[2], gameDataService.Growths);
            Add(files, gameDirectory, DataFolders[3], gameDataService.Actors);
            Add(files, gameDirectory, DataFolders[4], gameDataService.Disciplines);
            Add(files, gameDirectory, DataFolders[5], gameDataService.Artes);
            Add(files, gameDirectory, DataFolders[6], gameDataService.Consumables);
            Add(files, gameDirectory, DataFolders[7], gameDataService.Equipment);
            Add(files, gameDirectory, DataFolders[8], gameDataService.Enemies);
            Add(files, gameDirectory, DataFolders[9], gameDataService.Troops);
            Add(files, gameDirectory, DataFolders[10], gameDataService.States);
            Add(files, gameDirectory, DataFolders[11], gameDataService.Animations);
            Add(files, gameDirectory, DataFolders[12], gameDataService.GameMaps);
            files[Path.Combine(gameDirectory, GameStartFile)] = jsonService.Serialize(gameDataService.GameStart);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Nothing was saved, every file on disk is untouched. {exception.Message}", exception);
        }

        return files;
    }

    private void Add<T>(Dictionary<string, string> files, string gameDirectory, string folder, IEnumerable<T> collection) where T : BaseModel
    {
        foreach (var item in collection)
        {
            try
            {
                files[Path.Combine(gameDirectory, folder, $"{item.Guid}{DataExtension}")] = jsonService.Serialize(item);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{folder} {item.Id:000} ({item.Name}) could not be saved: {exception.Message}", exception);
            }
        }
    }

    #endregion

    #region Backup

    // the save on disk before this one, kept outside the game folder so it never ships with the game
    private void Backup(string gameDirectory)
    {
        if (locationService.MercuryGameMakerDirectory is null) locationService.CreateMercuryGameDirectory();
        var backups = Path.Combine(locationService.MercuryGameMakerDirectory!, "Backups", Path.GetFileName(Path.TrimEndingDirectorySeparator(gameDirectory)));
        var backup = Path.Combine(backups, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

        foreach (var folder in DataFolders)
        {
            var source = Path.Combine(gameDirectory, folder);
            if (!Directory.Exists(source)) continue;

            var target = Path.Combine(backup, folder);
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source, $"*{DataExtension}")) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        }

        var gameStart = Path.Combine(gameDirectory, GameStartFile);
        if (File.Exists(gameStart))
        {
            Directory.CreateDirectory(Path.Combine(backup, "Settings"));
            File.Copy(gameStart, Path.Combine(backup, GameStartFile), true);
        }

        // the names sort by date, so the oldest go first
        if (!Directory.Exists(backups)) return;
        foreach (var old in Directory.GetDirectories(backups).Order().SkipLast(BackupsKept)) Directory.Delete(old, true);
    }

    #endregion

    #region Write

    // written beside the old file and swapped in, a crash part way leaves the old file or the new one, never half of one
    private static void WriteFile(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + TempExtension;
        File.WriteAllText(temp, json);
        File.Move(temp, path, true);
    }

    // only runs after every file was written: removes the files of data deleted in the editor
    private static void RemoveDeletedData(string gameDirectory, IEnumerable<string> saved)
    {
        var keep = saved.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in DataFolders)
        {
            var directory = Path.Combine(gameDirectory, folder);
            if (!Directory.Exists(directory)) continue;

            foreach (var file in Directory.GetFiles(directory, $"*{DataExtension}").Where(x => !keep.Contains(x))) File.Delete(file);
            foreach (var file in Directory.GetFiles(directory, $"*{TempExtension}")) File.Delete(file);
        }
    }

    #endregion
}
