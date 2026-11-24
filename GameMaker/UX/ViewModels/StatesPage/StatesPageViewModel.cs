using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Models.States;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameMaker.UX.ViewModels.EquipmentPage;

namespace GameMaker.UX.ViewModels.StatesPage;

public class StatesPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<State>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    public ObservableCollection<StateChangeViewModel> AttributeChanges { get; } = [];

    public ObservableCollection<StateChangeViewModel> ElementChanges { get; } = [];

    // the other states this one keeps off its holder
    public ObservableCollection<StateResistViewModel> StateResists { get; } = [];

    public Guid? SelectedTickArte
    {
        get => SelectedEntity?.TickArte;
        set
        {
            if (SelectedEntity is null || SelectedEntity.TickArte == value) return;
            SelectedEntity.TickArte = value;
            OnPropertyChanged();
        }
    }

    public Guid? SelectedTickAnimation
    {
        get => SelectedEntity is null || SelectedEntity.TickAnimation == Guid.Empty
            ? null
            : SelectedEntity.TickAnimation;
        set
        {
            if (SelectedEntity is null || SelectedEntity.TickAnimation == (value ?? Guid.Empty)) return;
            SelectedEntity.TickAnimation = value ?? Guid.Empty;
            OnPropertyChanged();
        }
    }

    protected override ObservableCollection<State> EntityCollection => gameDataService.States;

    #endregion

    #region Actions

    protected override Task OnSelectedIndexChanged(int index)
    {
        RefreshStats();
        OnPropertyChanged(nameof(SelectedTickAnimation));
        OnPropertyChanged(nameof(SelectedTickArte));
        return Task.CompletedTask;
    }

    #endregion

    #region Private methods

    private void RefreshStats()
    {
        AttributeChanges.Clear();
        ElementChanges.Clear();
        StateResists.Clear();

        if (SelectedEntity is null) return;

        foreach (var attribute in GameDataService.Attributes) AttributeChanges.Add(new StateChangeViewModel(attribute, SelectedEntity, false));
        foreach (var element in GameDataService.Elements) ElementChanges.Add(new StateChangeViewModel(element, SelectedEntity, true));
        foreach (var state in GameDataService.States.Where(x => x != SelectedEntity)) StateResists.Add(new StateResistViewModel(state, SelectedEntity.ResistStates));
    }

    #endregion
}
