using System.Collections.ObjectModel;
using GameLibrary.Models;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Tasks;
using GameLibrary.Utilities.ComponentModels;

namespace GameMaker.Tasks;

public class LoadDataTask(ILocationService locationService, IJsonService jsonService, IGameDataService gameDataService) : EngineTask
{
    public override Task Call()
    {
        GatherWorkload();
        LoadData();
        Work = MaxWork;
        return Task.CompletedTask;
    }

    private void GatherWorkload()
    {
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Settings", "Attributes")).Length; // attributes
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Settings", "Elements")).Length; // elements
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Settings", "Growths")).Length; // growths
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Fighters", "Actors")).Length; // actors
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Disciplines")).Length; // disciplines
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Items", "Consumables")).Length; // consumables
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Items", "Equipment")).Length; // equipment
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "Fighters", "Enemies")).Length; // enemies
        MaxWork += Directory.GetFiles(Path.Combine(locationService.GameDirectory!, "States")).Length; // states
    }

    private void LoadData()
    {
        DecryptData(Path.Combine("Settings", "Attributes"), gameDataService.Attributes);
        DecryptData(Path.Combine("Settings", "Elements"), gameDataService.Elements);
        DecryptData(Path.Combine("Settings", "Growths"), gameDataService.Growths);
        DecryptData(Path.Combine("Fighters", "Actors"), gameDataService.Actors);
        DecryptData(Path.Combine("Disciplines"), gameDataService.Disciplines);
        DecryptData(Path.Combine("Items", "Consumables"), gameDataService.Consumables);
        DecryptData(Path.Combine("Items", "Equipment"), gameDataService.Equipment);
        DecryptData(Path.Combine("Fighters", "Enemies"), gameDataService.Enemies);
        DecryptData(Path.Combine("States"), gameDataService.States);
    }

    private void DecryptData<T>(string path, ObservableCollection<T> collection) where T : BaseModel
    {
        var directoryPath = Path.Combine(locationService.GameDirectory!, path);
        foreach (var entityFile in Directory.GetFiles(directoryPath))
        {
            if (!entityFile.EndsWith(".data")) continue;
            var entity = jsonService.DecryptData<T>(entityFile);
            if (entity is null)
            {
                Work++;
                continue;
            }

            collection.Add(entity);
            Work++;
        }

        collection.OrderBy(x => x.Id).Apply();
    }
}
