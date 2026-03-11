using System.Collections.ObjectModel;
using System.ComponentModel;
using GameLibrary.Models.Disciplines;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.DisciplinesPage;

public class AttributeGrowthSetting : PropertyChangedUpdater
{
    public Guid AttributeGuid { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Icon { get; set; }

    public Guid GrowthGuid
    {
        get;
        set => SetField(ref field, value);
    }
}

public class DisciplinesPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<Discipline>(jsonService)
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
        LoadAttributeSettings();
    }

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        LoadAttributeSettings();
    }

    private void LoadAttributeSettings()
    {
        foreach (var setting in AttributeSettings)
        {
            setting.PropertyChanged -= OnSettingPropertyChanged;
        }

        AttributeSettings.Clear();
        if (SelectedEntity == null) return;

        // Life
        var lifeSetting = new AttributeGrowthSetting
        {
            AttributeGuid = Guid.Empty, // Special case or use a specific fixed GUID if exists
            Name = "Life",
            Icon = 0, // Should probably be a specific icon
            GrowthGuid = SelectedEntity.LifeGrowthGuid
        };
        lifeSetting.PropertyChanged += OnSettingPropertyChanged;
        AttributeSettings.Add(lifeSetting);

        // Mana
        var manaSetting = new AttributeGrowthSetting
        {
            AttributeGuid = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Example fixed GUID
            Name = "Mana",
            Icon = 1,
            GrowthGuid = SelectedEntity.ManaGrowthGuid
        };
        manaSetting.PropertyChanged += OnSettingPropertyChanged;
        AttributeSettings.Add(manaSetting);

        foreach (var attr in gameDataService.Attributes)
        {
            SelectedEntity.AttributeGrowths.TryGetValue(attr.Guid, out var growthGuid);
            var setting = new AttributeGrowthSetting
            {
                AttributeGuid = attr.Guid,
                Name = attr.Name,
                Icon = attr.Icon,
                GrowthGuid = growthGuid
            };
            setting.PropertyChanged += OnSettingPropertyChanged;
            AttributeSettings.Add(setting);
        }
    }

    private void OnSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AttributeGrowthSetting setting || SelectedEntity == null) return;

        if (setting.Name == "Life")
        {
            SelectedEntity.LifeGrowthGuid = setting.GrowthGuid;
        }
        else if (setting.Name == "Mana")
        {
            SelectedEntity.ManaGrowthGuid = setting.GrowthGuid;
        }
        else
        {
            SelectedEntity.AttributeGrowths[setting.AttributeGuid] = setting.GrowthGuid;
        }
    }

    #endregion
}
