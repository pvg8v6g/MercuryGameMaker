using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Models.States;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;

namespace GameMaker.UX.ViewModels.StatesPage;

public class StatesPageViewModel(IGameDataService gameDataService, IJsonService jsonService) : BaseViewModel<State>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    public ObservableCollection<StateChangeViewModel> AttributeChanges { get; } = [];

    public ObservableCollection<StateChangeViewModel> ElementChanges { get; } = [];

    public Guid? SelectedTickArte
    {
        get => SelectedEntity?.TickArte;
        set
        {
            if (SelectedEntity is null) return;
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
            if (SelectedEntity is null) return;
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

        if (SelectedEntity is null) return;

        foreach (var attribute in GameDataService.Attributes) AttributeChanges.Add(new StateChangeViewModel(attribute, SelectedEntity, false));
        foreach (var element in GameDataService.Elements) ElementChanges.Add(new StateChangeViewModel(element, SelectedEntity, true));
    }

    #endregion
}
