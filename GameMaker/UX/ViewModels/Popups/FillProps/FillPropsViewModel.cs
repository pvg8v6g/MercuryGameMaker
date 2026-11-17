using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Props;
using GameLibrary.Services.GameData;
using GameLibrary.Utilities.Calculations;
using GameMaker.UX.Models.MainPage;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.FillProps;

/// <summary>
/// The fill popup: pick or make a fill set (Forest, Desert, Snow), edit its rules, pick a seed and fill the map.
/// The fill sets are game data, so they are saved with the game and work on every map.
/// </summary>
public partial class FillPropsViewModel : PropertyChangedUpdater
{
    #region Fields

    private readonly IGameDataService _gameDataService;
    private readonly PropChoiceModel[] _props;
    private readonly string[] _areaNames;

    #endregion

    #region Constructor

    public FillPropsViewModel(IGameDataService gameDataService, IEnumerable<PropChoiceModel> props, IEnumerable<string> areaNames, string mapName)
    {
        _gameDataService = gameDataService;
        _props = [.. props];
        _areaNames = [.. areaNames.Where(x => !string.IsNullOrEmpty(x)).Distinct().Order()];
        MapName = mapName;
        SelectedFillSet = FillSets.FirstOrDefault();
    }

    #endregion

    #region Properties

    public string MapName { get; }

    public ObservableCollection<FillSet> FillSets => _gameDataService.FillSets;

    public FillSet? SelectedFillSet
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(HasFillSet));
            RefreshRules();
        }
    }

    public bool HasFillSet => SelectedFillSet is not null;

    public ObservableCollection<FillRuleModel> Rules { get; } = [];

    // the same seed always gives the same layout, shuffle for another one
    public double Seed
    {
        get;
        set => SetField(ref field, double.IsNaN(value) ? 1 : Math.Round(value));
    } = Random.Shared.Next(1, 100000);

    public bool ClearFirst
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public RelayCommand NewFillSetCommand => field ??= new RelayCommand(NewFillSet);

    public RelayCommand DeleteFillSetCommand => field ??= new RelayCommand(DeleteFillSet);

    public RelayCommand AddRuleCommand => field ??= new RelayCommand(AddRule);

    public RelayCommand ShuffleCommand => field ??= new RelayCommand(() => Seed = Random.Shared.Next(1, 100000));

    #endregion

    #region Actions

    // a new set starts with the two usual rules: something thick on unpassable ground, something sparse on open ground
    private void NewFillSet()
    {
        var fillSet = new FillSet
        {
            Id = Calculations.GetNextId([.. FillSets.Select(x => x.Id)]),
            Name = "New Fill Set",
            Rules =
            {
                new FillRule { Name = "Unpassable", Place = FillPlace.Unpassable, Density = 90, Spacing = 40 },
                new FillRule { Name = "Open ground", Place = FillPlace.Open, Density = 15, Spacing = 24, ClumpSize = 4 }
            }
        };
        FillSets.Add(fillSet);
        SelectedFillSet = fillSet;
    }

    private void DeleteFillSet()
    {
        if (SelectedFillSet is not { } fillSet) return;
        FillSets.Remove(fillSet);
        SelectedFillSet = FillSets.FirstOrDefault();
    }

    private void AddRule()
    {
        if (SelectedFillSet is not { } fillSet) return;
        var rule = new FillRule();
        fillSet.Rules.Add(rule);
        Rules.Add(new FillRuleModel(rule, _props, _areaNames, RemoveRule));
    }

    private void RemoveRule(FillRuleModel model)
    {
        SelectedFillSet?.Rules.Remove(model.Rule);
        Rules.Remove(model);
    }

    private void RefreshRules()
    {
        Rules.Clear();
        foreach (var rule in SelectedFillSet?.Rules ?? []) Rules.Add(new FillRuleModel(rule, _props, _areaNames, RemoveRule));
    }

    #endregion
}
