using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Models.Growths;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.Calculations;
using GameMaker.UX.Models.GrowthsPage;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GameMaker.UX.ViewModels.GrowthsPage;

public partial class GrowthsPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<Growth>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Growth> EntityCollection => gameDataService.Growths;

    public string GrowthLower
    {
        get;
        set => SetField(ref field, value);
    } = "10";

    public string GrowthUpper
    {
        get;
        set => SetField(ref field, value);
    } = "250";

    public double Variance
    {
        get;
        set => SetField(ref field, value);
    } = 0;

    public ISeries[] Series { get; set; } = [];

    public Axis[] XAxes { get; set; } =
    [
        new Axis
        {
            Name = "Level", MinLimit = 0, MaxLimit = 99, LabelsPaint = new SolidColorPaint(SKColors.White),
            NamePaint = new SolidColorPaint(SKColors.White), SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
        }
    ];

    public Axis[] YAxes { get; set; } =
    [
        new Axis
        {
            Name = "Value", MinStep = 10, LabelsPaint = new SolidColorPaint(SKColors.White), NamePaint = new SolidColorPaint(SKColors.White),
            SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
        }
    ];

    public ObservableCollection<GrowthDataEntry> GrowthDataList { get; } = [];

    #endregion

    #region Relays

    public RelayCommand RenderCommand => new(RenderAction);

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        var levelCap = gameDataService.LevelCap;
        var customSeparators = new double[] { 0, 20, 40, 60, 80, levelCap }.Distinct().OrderBy(x => x).ToArray();

        XAxes =
        [
            new Axis
            {
                Name = "Level", MinLimit = 0, MaxLimit = levelCap, LabelsPaint = new SolidColorPaint(SKColors.White),
                NamePaint = new SolidColorPaint(SKColors.White), CustomSeparators = customSeparators,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
            }
        ];
        YAxes =
        [
            new Axis
            {
                Name = "Value", MinStep = 10, LabelsPaint = new SolidColorPaint(SKColors.White), NamePaint = new SolidColorPaint(SKColors.White),
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
            }
        ];
        OnPropertyChanged(nameof(XAxes));
        OnPropertyChanged(nameof(YAxes));
        return Task.CompletedTask;
    }

    protected override async Task OnSelectedIndexChanged(int index)
    {
        await base.OnSelectedIndexChanged(index);
        var levelCap = gameDataService.LevelCap;
        var customSeparators = new double[] { 0, 20, 40, 60, 80, levelCap }.Distinct().OrderBy(x => x).ToArray();

        XAxes =
        [
            new Axis
            {
                Name = "Level", MinLimit = 0, MaxLimit = levelCap, LabelsPaint = new SolidColorPaint(SKColors.White),
                NamePaint = new SolidColorPaint(SKColors.White), CustomSeparators = customSeparators,
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
            }
        ];
        YAxes =
        [
            new Axis
            {
                Name = "Value", MinStep = 10, LabelsPaint = new SolidColorPaint(SKColors.White), NamePaint = new SolidColorPaint(SKColors.White),
                SeparatorsPaint = new SolidColorPaint(SKColors.Gray.WithAlpha(50), 0.5f)
            }
        ];
        OnPropertyChanged(nameof(XAxes));
        OnPropertyChanged(nameof(YAxes));
        UpdateChart();
    }

    private void RenderAction()
    {
        if (SelectedEntity is null) return;

        SelectedEntity.GrowthValues.Clear();
        if (!double.TryParse(GrowthLower, out var lower)) lower = 10;
        if (!double.TryParse(GrowthUpper, out var upper)) upper = 250;

        var v = -Variance;
        var rate = Calculations.RandomBetween(v - 0.125, v + 0.125);

        var levelCap = gameDataService.LevelCap;
        for (var x = 0; x <= levelCap; x++)
        {
            var y = (int) Calculations.GetLevelValue(x, rate, lower, upper);
            y = Math.Max(y, 0);
            if (SelectedEntity.GrowthValues.TryGetValue(x - 1, out var previous) && previous > y)
            {
                y = previous;
            }

            SelectedEntity.GrowthValues[x] = y;
        }

        UpdateChart();
    }

    #endregion

    #region Private Methods

    private void UpdateChart()
    {
        GrowthDataList.Clear();
        if (SelectedEntity is null || SelectedEntity.GrowthValues.Count == 0)
        {
            Series = [];
            OnPropertyChanged(nameof(Series));
            return;
        }

        var orderedValues = SelectedEntity.GrowthValues.OrderBy(x => x.Key).ToList();
        var values = orderedValues.Select(x => x.Value).ToArray();

        foreach (var entry in orderedValues.Where(entry => entry.Key != 0))
        {
            GrowthDataList.Add(new GrowthDataEntry(entry.Key, entry.Value));
        }

        Series =
        [
            new LineSeries<int>
            {
                Values = values,
                Fill = new SolidColorPaint(new SKColor(231, 7, 86).WithAlpha(100)),
                Stroke = new SolidColorPaint(new SKColor(231, 7, 86), 2),
                GeometrySize = 0,
                LineSmoothness = 0.5f,
                Name = SelectedEntity.Name
            }
        ];

        OnPropertyChanged(nameof(Series));
    }

    #endregion
}
