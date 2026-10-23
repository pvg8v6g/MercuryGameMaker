using System.Collections.ObjectModel;
using System.Text.Json;
using GameLibrary.Commands;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Movements;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameMaker.Services.Navigation;
using GameMaker.UX.ViewModels.Popups.MovementEditor;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.ViewModels.Popups.MovementRouteEditor;

public class MovementRouteEditorViewModel(
    EntityMovementAction action,
    IGameDataService gameDataService,
    INavigationService navigationService) : PropertyChangedUpdater
{
    #region Properties

    public ObservableCollection<Movement> Route => action.Route;

    public bool WaitForCompletion
    {
        get => action.WaitForCompletion;
        set
        {
            if (action.WaitForCompletion == value) return;
            action.WaitForCompletion = value;
            OnPropertyChanged();
        }
    }

    public bool SkipIfInterrupted
    {
        get => action.SkipIfInterrupted;
        set
        {
            if (action.SkipIfInterrupted == value) return;
            action.SkipIfInterrupted = value;
            OnPropertyChanged();
        }
    }

    public bool Repeat
    {
        get => action.Repeat;
        set
        {
            if (action.Repeat == value) return;
            action.Repeat = value;
            OnPropertyChanged();
        }
    }

    public int SelectedMovementIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    public Movement? CopiedMovement
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Relays

    public AsyncRelayCommand<string> AddMovementCommand => new(AddMovement);

    public AsyncRelayCommand EditMovementCommand => new(EditMovement);

    public RelayCommand CopyMovementCommand => new(CopyMovement);

    public RelayCommand PasteMovementCommand => new(PasteMovement);

    public RelayCommand DeleteMovementCommand => new(DeleteMovement);

    #endregion

    #region Actions

    private async Task AddMovement(string key)
    {
        var movement = CreateMovement(key);
        if (movement is null) return;

        if (IsParameterized(movement))
        {
            var result = await ShowEditor(movement);
            if (result != ContentDialogResult.Primary) return;
        }

        Route.Add(movement);
        SelectedMovementIndex = Route.Count - 1;
    }

    private async Task EditMovement()
    {
        var movement = SelectedMovement();
        if (movement is null) return;
        if (!IsParameterized(movement)) return;

        await ShowEditor(movement);
    }

    private void CopyMovement()
    {
        CopiedMovement = SelectedMovement();
    }

    private void PasteMovement()
    {
        if (CopiedMovement is null) return;

        var json = JsonSerializer.Serialize<Movement>(CopiedMovement);
        var clone = JsonSerializer.Deserialize<Movement>(json);
        if (clone is null) return;

        clone.Guid = Guid.NewGuid();
        Route.Add(clone);
        SelectedMovementIndex = Route.Count - 1;
    }

    private void DeleteMovement()
    {
        var index = SelectedMovementIndex;
        if (index < 0 || index >= Route.Count) return;

        Route.RemoveAt(index);

        if (Route.Count == 0)
        {
            SelectedMovementIndex = -1;
        }
        else if (index >= Route.Count)
        {
            SelectedMovementIndex = Route.Count - 1;
        }
        else
        {
            SelectedMovementIndex = index;
        }
    }

    #endregion

    #region Private methods

    private Movement? SelectedMovement()
    {
        if (SelectedMovementIndex < 0 || SelectedMovementIndex >= Route.Count) return null;
        return Route[SelectedMovementIndex];
    }

    private async Task<ContentDialogResult> ShowEditor(Movement movement)
    {
        var viewModel = new MovementEditorViewModel(movement, gameDataService.Artes);
        return await navigationService.ShowMovementEditorPopup(viewModel);
    }

    private static bool IsParameterized(Movement movement) => movement is
        MoveMovement or
        MoveTowardMovement or
        MoveAwayMovement or
        SpeedMovement or
        OpacityMovement or
        WaitMovement or
        TurnMovement or
        BlendModeMovement or
        CharacterImageMovement or
        ActivateArteMovement;

    private static Movement? CreateMovement(string key) => key switch
    {
        "Move" => new MoveMovement(),
        "MoveRandom" => new MoveRandomMovement(),
        "MoveToward" => new MoveTowardMovement(),
        "MoveAway" => new MoveAwayMovement(),
        "StepForward" => new StepForwardMovement(),
        "StepAway" => new StepAwayMovement(),
        "Speed" => new SpeedMovement(),
        "Turn" => new TurnMovement(),
        "TurnRandom" => new TurnRandomMovement(),
        "Turn180" => new Turn180Movement(),
        "TurnToward" => new TurnTowardEntityMovement(),
        "TurnAway" => new TurnAwayEntityMovement(),
        "Stepping" => new SteppingMovement(),
        "DirectionFix" => new DirectionFixMovement(),
        "Opacity" => new OpacityMovement(),
        "Through" => new ThroughMovement(),
        "Blend" => new BlendModeMovement(),
        "CharacterImage" => new CharacterImageMovement(),
        "Wait" => new WaitMovement(),
        "Detonate" => new DetonateMovement(),
        "ActivateArte" => new ActivateArteMovement(),
        _ => null
    };

    #endregion
}
