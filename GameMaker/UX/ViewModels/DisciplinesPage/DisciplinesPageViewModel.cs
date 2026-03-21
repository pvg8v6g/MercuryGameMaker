using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using GameLibrary.Models.Disciplines;
using GameLibrary.Models.Growths;
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

    private Dictionary<Guid, Growth> _growthLookup = new();
    private Dictionary<Guid, int[]> _growthValueArrays = new();

    public ObservableCollection<AttributeGrowthSetting> AttributeSettings { get; } = new();

    #endregion

    #region Overrides

    protected override async Task OnSelectedIndexChanged(int index)
    {
        await base.OnSelectedIndexChanged(index);
        UpdateAttributeGrowths();
    }

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        if (AttributeSettings.Count == 0)
        {
            CreateAttributeSettings();
        }
        if (SelectedEntity is not null)
        {
            UpdateAttributeGrowths();
        }
    }

    #endregion

    #region Private Methods

    private void CreateAttributeSettings()
    {
        _growthLookup = GameDataService.Growths.ToDictionary(g => g.Guid);

        var lifeSetting = new AttributeGrowthSetting
        {
            AvailableGrowths = GameDataService.Growths,
            IconIndex = 478,
            AttributeGuid = GameDataService.LifeAttributeGuid,
            Name = "Life",
            GrowthGuid = null
        };
        InitializeSetting(lifeSetting);
        AttributeSettings.Add(lifeSetting);

        var manaSetting = new AttributeGrowthSetting
        {
            AvailableGrowths = GameDataService.Growths,
            IconIndex = 523,
            AttributeGuid = GameDataService.ManaAttributeGuid,
            Name = "Mana",
            GrowthGuid = null
        };
        InitializeSetting(manaSetting);
        AttributeSettings.Add(manaSetting);

        foreach (var attr in GameDataService.Attributes)
        {
            var setting = new AttributeGrowthSetting
            {
                AvailableGrowths = GameDataService.Growths,
                IconIndex = attr.Icon,
                AttributeGuid = attr.Guid,
                Name = attr.Name,
                GrowthGuid = null
            };
            InitializeSetting(setting);
            AttributeSettings.Add(setting);
        }
    }

    private void UpdateAttributeGrowths()
    {
        if (SelectedEntity is null)
        {
            foreach (var setting in AttributeSettings)
            {
                setting.GrowthGuid = null;
            }
            return;
        }

        foreach (var setting in AttributeSettings)
        {
            Guid? growthGuid = setting.AttributeGuid switch
            {
                var guid when guid == GameDataService.LifeAttributeGuid => SelectedEntity.LifeGrowthGuid,
                var guid when guid == GameDataService.ManaAttributeGuid => SelectedEntity.ManaGrowthGuid,
                _ => SelectedEntity.AttributeGrowths.TryGetValue(setting.AttributeGuid, out var g) ? g : null
            };
            setting.GrowthGuid = growthGuid;
        }
    }

    private void InitializeSetting(AttributeGrowthSetting setting)
    {
        setting.XAxes =
        [
            new Axis
            {
                MinLimit = 0, MaxLimit = GameDataService.LevelCap,
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
        if (targetGuid == Guid.Empty)
        {
            setting.Series = [];
            return;
        }

        if (!_growthLookup.TryGetValue(targetGuid, out var growth) || growth.GrowthValues.Count == 0)
        {
            setting.Series = [];
            return;
        }

        if (!_growthValueArrays.TryGetValue(targetGuid, out var values))
        {
            values = growth.GrowthValues.OrderBy(x => x.Key).Select(x => x.Value).ToArray();
            _growthValueArrays[targetGuid] = values;
        }

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
        if (setting.AttributeGuid == GameDataService.LifeAttributeGuid)
        {
            SelectedEntity.LifeGrowthGuid = growthGuid;
        }
        else if (setting.AttributeGuid == GameDataService.ManaAttributeGuid)
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
