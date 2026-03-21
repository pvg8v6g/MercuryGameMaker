using System.Collections.ObjectModel;
using GameLibrary.Models.Items;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameMaker.UX.ViewModels.EquipmentPage;

public class EquipmentPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<Equipment>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    public ObservableCollection<AttributeStatViewModel> AttributeStats { get; } = [];

    public ObservableCollection<AttributeStatViewModel> ElementResistanceStats { get; } = [];

    public ObservableCollection<StateResistViewModel> StateResistanceStats { get; } = [];

    protected override ObservableCollection<Equipment> EntityCollection => gameDataService.Equipment;

    #endregion

    #region Actions

    protected override Task OnSelectedIndexChanged(int index)
    {
        RefreshStats();
        return Task.CompletedTask;
    }

    #endregion

    #region Private methods

    private void RefreshStats()
    {
        AttributeStats.Clear();
        ElementResistanceStats.Clear();
        StateResistanceStats.Clear();

        if (SelectedEntity is null) return;

        foreach (var attribute in GameDataService.Attributes)
        {
            AttributeStats.Add(new AttributeStatViewModel(attribute, SelectedEntity));
        }

        foreach (var element in GameDataService.Elements)
        {
            ElementResistanceStats.Add(new AttributeStatViewModel(element, SelectedEntity));
        }

        foreach (var state in GameDataService.States)
        {
            StateResistanceStats.Add(new StateResistViewModel(state, SelectedEntity));
        }

        OnPropertyChanged(nameof(AttributeStats));
        OnPropertyChanged(nameof(ElementResistanceStats));
        OnPropertyChanged(nameof(StateResistanceStats));
    }

    #endregion
}
