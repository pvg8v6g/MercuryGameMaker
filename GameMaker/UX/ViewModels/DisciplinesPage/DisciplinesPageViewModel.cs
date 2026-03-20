using System.Collections.ObjectModel;
using System.ComponentModel;
using GameLibrary.Models.Disciplines;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameMaker.UX.Models.DisciplinesPage;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GameMaker.UX.ViewModels.DisciplinesPage;

public class DisciplinesPageViewModel(IGameDataService gameDataService, IJsonService jsonService)
    : BaseViewModel<Discipline>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Discipline> EntityCollection => gameDataService.Disciplines;

    public ObservableCollection<AttributeGrowthSetting> AttributeSettings { get; } = new();

    #endregion

    #region Overrides

    protected override async Task OnSelectedIndexChanged(int index)
    {
        await base.OnSelectedIndexChanged(index);
        foreach (var setting in AttributeSettings)
        {
            setting.PropertyChanged -= OnSettingPropertyChanged;
        }

        AttributeSettings.Clear();
        if (SelectedEntity is null) return;
        await LoadAttributeSettings(SelectedEntity);
    }

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        if (SelectedEntity is null) return;
        await LoadAttributeSettings(SelectedEntity);
    }

    #endregion

    #region Private Methods

    private async Task LoadAttributeSettings(Discipline discipline)
    {
        var lifeSetting = new AttributeGrowthSetting
        {
            AvailableGrowths = gameDataService.Growths,
            IconIndex = 478,
            AttributeGuid = gameDataService.LifeAttributeGuid,
            Name = "Life",
            GrowthGuid = discipline.LifeGrowthGuid
        };
        InitializeSetting(lifeSetting);
        AttributeSettings.Add(lifeSetting);

        var manaSetting = new AttributeGrowthSetting
        {
            AvailableGrowths = gameDataService.Growths,
            IconIndex = 523,
            AttributeGuid = gameDataService.ManaAttributeGuid,
            Name = "Mana",
            GrowthGuid = discipline.ManaGrowthGuid
        };
        InitializeSetting(manaSetting);
        AttributeSettings.Add(manaSetting);

        foreach (var attr in gameDataService.Attributes)
        {
            discipline.AttributeGrowths.TryGetValue(attr.Guid, out var growthGuid);
            var setting = new AttributeGrowthSetting
            {
                AvailableGrowths = gameDataService.Growths,
                IconIndex = attr.Icon,
                AttributeGuid = attr.Guid,
                Name = attr.Name,
                GrowthGuid = growthGuid
            };
            InitializeSetting(setting);
            AttributeSettings.Add(setting);
        }
    }

    private void InitializeSetting(AttributeGrowthSetting setting)
    {
        setting.XAxes =
        [
            new Axis
            {
                MinLimit = 0, MaxLimit = gameDataService.LevelCap,
                LabelsPaint = null,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
            }
        ];
        setting.YAxes =
        [
            new Axis
            {
                LabelsPaint = null,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
            }
        ];
        UpdateSettingChart(setting);
        setting.PropertyChanged += OnSettingPropertyChanged;
    }

    private void UpdateSettingChart(AttributeGrowthSetting setting)
    {
        var targetGuid = setting.GrowthGuid ?? Guid.Empty;
        var growth = gameDataService.Growths.FirstOrDefault(x => x.Guid == targetGuid);
        if (growth is null || growth.GrowthValues.Count == 0)
        {
            setting.Series = [];
            return;
        }

        var values = growth.GrowthValues.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

        setting.Series =
        [
            new LineSeries<int>
            {
                Values = values,
                Fill = new SolidColorPaint(new SKColor(231, 7, 86).WithAlpha(100)),
                Stroke = new SolidColorPaint(new SKColor(231, 7, 86), 2),
                GeometrySize = 0,
                LineSmoothness = 0.5f,
                Name = growth.Name
            }
        ];
    }

    private void OnSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AttributeGrowthSetting setting || SelectedEntity == null) return;

        if (e.PropertyName is not nameof(AttributeGrowthSetting.GrowthGuid)) return;
        var growthGuid = setting.GrowthGuid ?? Guid.Empty;
        if (setting.AttributeGuid == gameDataService.LifeAttributeGuid)
        {
            SelectedEntity.LifeGrowthGuid = growthGuid;
        }
        else if (setting.AttributeGuid == gameDataService.ManaAttributeGuid)
        {
            SelectedEntity.ManaGrowthGuid = growthGuid;
        }
        else
        {
            SelectedEntity.AttributeGrowths[setting.AttributeGuid] = growthGuid;
        }

        UpdateSettingChart(setting);
    }

    #endregion
}
