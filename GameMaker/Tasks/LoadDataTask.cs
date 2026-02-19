using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Tasks;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.Tasks;

public class LoadDataTask(ILocationService locationService, IJsonService jsonService, IGameDataService gameDataService) : EngineTask
{
    #region Fields

    private string[] _attributeFiles = [];

    #endregion

    public override Task Call()
    {
        GatherWorkload();
        LoadData();
        Work = MaxWork;
        return Task.CompletedTask;
    }

    private void GatherWorkload()
    {
        var attributeDirectory = Path.Combine(locationService.GameDirectory!, "Settings", "Attributes");
        _attributeFiles = Directory.GetFiles(attributeDirectory).Where(x => x.EndsWith(".data")).ToArray();
        MaxWork += _attributeFiles.Length;
    }

    private void LoadData()
    {
        foreach (var attributeFile in _attributeFiles)
        {
            var attribute = jsonService.DecryptData<Attribute>(attributeFile);
            if (attribute is null)
            {
                Work++;
                continue;
            }

            gameDataService.Attributes.Add(attribute);
            Work++;
        }
    }
}
