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

    public ObservableCollection<ElementEffectViewModel> ElementEffects { get; } = [];

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

    public RelayCommand ClearTickAnimationCommand => new(ClearTickAnimation);

    #endregion

    #region Actions

    private void ClearTickAnimation()
    {
        if (SelectedEntity is null) return;
        SelectedEntity.TickAnimation = Guid.Empty;
        OnPropertyChanged(nameof(SelectedTickAnimation));
    }

    protected override Task OnSelectedIndexChanged(int index)
    {
        RefreshStats();
        OnPropertyChanged(nameof(SelectedTickAnimation));
        return Task.CompletedTask;
    }

    #endregion

    #region Private methods

    private void RefreshStats()
    {
        ElementEffects.Clear();

        if (SelectedEntity is null) return;

        foreach (var element in GameDataService.Elements)
        {
            ElementEffects.Add(new ElementEffectViewModel(element, SelectedEntity));
        }

        OnPropertyChanged(nameof(ElementEffects));
    }

    #endregion
}
