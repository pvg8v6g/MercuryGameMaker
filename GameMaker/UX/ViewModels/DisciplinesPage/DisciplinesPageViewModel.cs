using System.Collections.ObjectModel;
using GameLibrary.Models.Disciplines;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameMaker.UX.Models.DisciplinesPage;

namespace GameMaker.UX.ViewModels.DisciplinesPage;

public class DisciplinesPageViewModel(IGameDataService gameDataService, IJsonService jsonService, IGraphicsService graphicsService)
    : BaseViewModel<Discipline>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Discipline> EntityCollection => gameDataService.Disciplines;

    public ObservableCollection<AttributeGrowthSetting> AttributeSettings { get; } = new();

    #endregion

    #region Overrides

    protected override void OnSelectedIndexChanged(int index)
    {
        base.OnSelectedIndexChanged(index);
        AttributeSettings.Clear();
        if (SelectedEntity is null) return;
        LoadAttributeSettings(SelectedEntity);
    }

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
    }

    // private void LoadAttributeSettings()
    // {
    //     foreach (var setting in AttributeSettings)
    //     {
    //         setting.PropertyChanged -= OnSettingPropertyChanged;
    //     }
    //
    //     AttributeSettings.Clear();
    //     if (SelectedEntity == null) return;
    //
    //     // Life
    //     var lifeSetting = new AttributeGrowthSetting
    //     {
    //         AttributeGuid = Guid.Empty, // Special case or use a specific fixed GUID if exists
    //         Name = "Life",
    //         Icon = 0, // Should probably be a specific icon
    //         GrowthGuid = SelectedEntity.LifeGrowthGuid
    //     };
    //     lifeSetting.PropertyChanged += OnSettingPropertyChanged;
    //     AttributeSettings.Add(lifeSetting);
    //
    //     // Mana
    //     var manaSetting = new AttributeGrowthSetting
    //     {
    //         AttributeGuid = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Example fixed GUID
    //         Name = "Mana",
    //         Icon = 1,
    //         GrowthGuid = SelectedEntity.ManaGrowthGuid
    //     };
    //     manaSetting.PropertyChanged += OnSettingPropertyChanged;
    //     AttributeSettings.Add(manaSetting);
    //
    //     foreach (var attr in gameDataService.Attributes)
    //     {
    //         SelectedEntity.AttributeGrowths.TryGetValue(attr.Guid, out var growthGuid);
    //         var setting = new AttributeGrowthSetting
    //         {
    //             AttributeGuid = attr.Guid,
    //             Name = attr.Name,
    //             Icon = attr.Icon,
    //             GrowthGuid = growthGuid
    //         };
    //         setting.PropertyChanged += OnSettingPropertyChanged;
    //         AttributeSettings.Add(setting);
    //     }
    // }
    //
    // private void OnSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    // {
    //     if (sender is not AttributeGrowthSetting setting || SelectedEntity == null) return;
    //
    //     if (setting.Name == "Life")
    //     {
    //         SelectedEntity.LifeGrowthGuid = setting.GrowthGuid;
    //     }
    //     else if (setting.Name == "Mana")
    //     {
    //         SelectedEntity.ManaGrowthGuid = setting.GrowthGuid;
    //     }
    //     else
    //     {
    //         SelectedEntity.AttributeGrowths[setting.AttributeGuid] = setting.GrowthGuid;
    //     }
    // }

    #endregion

    #region Private Methods

    private async Task LoadAttributeSettings(Discipline discipline)
    {
        var lifeSetting = new AttributeGrowthSetting
        {
            IconImage = await graphicsService.GetIcon(478),
            AttributeGuid = gameDataService.LifeAttributeGuid,
            Name = "Life",
            GrowthGuid = discipline.LifeGrowthGuid
        };
        AttributeSettings.Add(lifeSetting);

        var manaSetting = new AttributeGrowthSetting
        {
            IconImage = await graphicsService.GetIcon(523),
            AttributeGuid = gameDataService.ManaAttributeGuid,
            Name = "Mana",
            GrowthGuid = discipline.ManaGrowthGuid
        };
        AttributeSettings.Add(manaSetting);
    }

    #endregion
}
