using System.Collections.ObjectModel;
using GameLibrary.Commands;
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

    // All first, then a tick box per discipline
    public ObservableCollection<DisciplineToggleViewModel> DisciplineToggles { get; } = [];

    // the ticked disciplines in a line, for the dropdown button
    public string EquippableByText
    {
        get
        {
            if (SelectedEntity is null || SelectedEntity.Disciplines.Count == 0) return "All";

            var names = gameDataService.Disciplines.Where(x => SelectedEntity.Disciplines.Contains(x.Guid)).OrderBy(x => x.Id).Select(x => x.Name);
            return string.Join(", ", names);
        }
    }

    #endregion

    #region Actions

    protected override Task OnSelectedIndexChanged(int index)
    {
        RefreshStats();
        return Task.CompletedTask;
    }

    // a tick changed: every box and the button text follow, since All and the disciplines affect each other
    private void DisciplinesChanged()
    {
        foreach (var toggle in DisciplineToggles) toggle.Refresh();
        OnPropertyChanged(nameof(EquippableByText));
    }

    #endregion

    #region Private methods

    private void RefreshStats()
    {
        AttributeStats.Clear();
        ElementResistanceStats.Clear();
        StateResistanceStats.Clear();
        DisciplineToggles.Clear();
        OnPropertyChanged(nameof(EquippableByText));

        if (SelectedEntity is null) return;

        DisciplineToggles.Add(new DisciplineToggleViewModel(null, SelectedEntity, DisciplinesChanged));
        foreach (var discipline in gameDataService.Disciplines.OrderBy(x => x.Id))
        {
            DisciplineToggles.Add(new DisciplineToggleViewModel(discipline, SelectedEntity, DisciplinesChanged));
        }

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
