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
        LoadData();
        Work = MaxWork;
        return Task.CompletedTask;
    }

    private void GatherWorkload()
    {
        MaxWork += gameDataService.Attributes.Count;
    }

    private void LoadData()
    {
        var attributeDirectory = Path.Combine(locationService.GameDirectory!, "Settings", "Attributes");
        foreach (var attribute in gameDataService.Attributes)
        {
            jsonService.EncryptFile(attribute, Path.Combine(attributeDirectory, $"{attribute.Guid}.data"));
            Work++;
        }
    }
}
