using System.Collections.ObjectModel;
using GameLibrary.Models;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Tasks;

namespace GameMaker.Tasks;

public class SaveDataTask(ILocationService locationService, IGameDataService gameDataService, IJsonService jsonService) : EngineTask
{
    public override Task Call()
    {
        GatherWorkload();
        SaveData();
        Work = MaxWork;
        return Task.CompletedTask;
    }

    private void GatherWorkload()
    {
        MaxWork += gameDataService.Attributes.Count;
        MaxWork += gameDataService.Elements.Count;
        MaxWork += gameDataService.Growths.Count;
        MaxWork += gameDataService.Disciplines.Count;
    }

    private void SaveData()
    {
        DataWork(Path.Combine(locationService.GameDirectory!, "Settings", "Attributes"), gameDataService.Attributes); // attributes
        DataWork(Path.Combine(locationService.GameDirectory!, "Settings", "Elements"), gameDataService.Elements); // elements
        DataWork(Path.Combine(locationService.GameDirectory!, "Settings", "Growths"), gameDataService.Growths); // growths
        DataWork(Path.Combine(locationService.GameDirectory!, "Disciplines"), gameDataService.Disciplines); // disciplines
    }

    private void DataWork<T>(string directory, ObservableCollection<T> collection) where T : BaseModel
    {
        DeleteOldData(directory);
        CreateNewData(directory, collection);
    }

    private void DeleteOldData(string directory)
    {
        var oldFiles = Directory.GetFiles(directory);
        foreach (var oldFile in oldFiles) File.Delete(oldFile);
    }

    private void CreateNewData<T>(string directory, ObservableCollection<T> collection) where T : BaseModel
    {
        foreach (var entity in collection)
        {
            jsonService.EncryptFile(entity, Path.Combine(directory, $"{entity.Guid}.data"));
            Work++;
        }
    }
}
