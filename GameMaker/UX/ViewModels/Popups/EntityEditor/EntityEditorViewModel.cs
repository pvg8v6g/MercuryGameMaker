using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Conditions;
using GameLibrary.Models.Modules;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.Calculations;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.Services.Navigation;
using GameMaker.UX.ViewModels.Popups.MovementRouteEditor;

namespace GameMaker.UX.ViewModels.Popups.EntityEditor;

public class EntityEditorViewModel : BaseViewModel<Page>
{
    #region Construction

    private readonly IGameDataService _gameDataService;
    private readonly INavigationService _navigationService;

    public EntityEditorViewModel(
        Entity entity,
        IGameDataService gameDataService,
        IJsonService jsonService,
        INavigationService navigationService) : base(jsonService)
    {
        Entity = entity;
        _gameDataService = gameDataService;
        _navigationService = navigationService;

        if (Entity.Pages.Count == 0)
        {
            NewEntity("New Page");
        }
        else
        {
            SelectedIndex = 0;
        }
    }

    #endregion

    #region Properties

    public Entity Entity { get; }

    public string EntityName
    {
        get => Entity.Name;
        set
        {
            if (Entity.Name == value) return;
            Entity.Name = value;
            OnPropertyChanged();
        }
    }

    public PageTrigger[] Triggers { get; } = Enum.GetValues<PageTrigger>();

    public Page? SelectedPage => SelectedEntity;

    public ObservableCollection<Condition>? SelectedConditions => SelectedPage?.Conditions;

    public ObservableCollection<PageAction>? SelectedActions => SelectedPage?.Actions;

    public ObservableCollection<Module>? SelectedModules => SelectedPage?.Modules;

    public int SelectedConditionIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    public int SelectedActionIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    protected override ObservableCollection<Page> EntityCollection => Entity.Pages;

    #endregion

    #region Relays

    public RelayCommand DeleteConditionCommand => new(DeleteCondition);

    public RelayCommand DeleteActionCommand => new(DeleteAction);

    public AsyncRelayCommand AddMovementRouteCommand => new(AddMovementRoute);

    public AsyncRelayCommand EditActionCommand => new(EditAction);

    #endregion

    #region Actions

    protected override void NewEntity(string? newEntityName = null)
    {
        newEntityName ??= "New Page";
        var newObject = new Page
        {
            Id = Calculations.GetNextId(EntityCollection.Select(x => x.Id).ToArray()),
            Name = newEntityName,
            EntityId = Entity.Guid
        };

        var index = EntityCollection.AddSorted(newObject, x => x.Id);
        SelectedIndex = index;
    }

    protected override Task OnSelectedIndexChanged(int index)
    {
        OnPropertyChanged(nameof(SelectedPage));
        OnPropertyChanged(nameof(SelectedConditions));
        OnPropertyChanged(nameof(SelectedActions));
        OnPropertyChanged(nameof(SelectedModules));
        SelectedConditionIndex = -1;
        SelectedActionIndex = -1;
        return Task.CompletedTask;
    }

    private void DeleteCondition()
    {
        if (SelectedConditions is null) return;
        if (SelectedConditionIndex < 0 || SelectedConditionIndex >= SelectedConditions.Count) return;
        SelectedConditions.RemoveAt(SelectedConditionIndex);
    }

    private void DeleteAction()
    {
        if (SelectedActions is null) return;
        if (SelectedActionIndex < 0 || SelectedActionIndex >= SelectedActions.Count) return;
        SelectedActions.RemoveAt(SelectedActionIndex);
    }

    private async Task AddMovementRoute()
    {
        if (SelectedActions is null) return;

        var action = new EntityMovementAction();
        SelectedActions.Add(action);
        SelectedActionIndex = SelectedActions.Count - 1;

        await ShowMovementRoute(action);
    }

    private async Task EditAction()
    {
        if (SelectedActions is null) return;
        if (SelectedActionIndex < 0 || SelectedActionIndex >= SelectedActions.Count) return;

        if (SelectedActions[SelectedActionIndex] is EntityMovementAction movementAction)
        {
            await ShowMovementRoute(movementAction);
        }
    }

    private async Task ShowMovementRoute(EntityMovementAction action)
    {
        var viewModel = new MovementRouteEditorViewModel(action, _gameDataService, _navigationService);
        await _navigationService.ShowMovementRouteEditorPopup(viewModel);
    }

    #endregion
}
