using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Conditions;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using GameLibrary.Services.GameData;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.ConditionEditor;

public class ConditionEditorViewModel(Condition condition, IGameDataService gameDataService) : PropertyChangedUpdater
{
    #region Properties

    public Condition Condition { get; } = condition;

    public ObservableCollection<Fighter> Actors => gameDataService.Actors;

    public ObservableCollection<Consumable> Consumables => gameDataService.Consumables;

    public SelfSwitch[] SelfSwitches { get; } = Enum.GetValues<SelfSwitch>();

    public SelfSwitchCondition? AsSelfSwitch => Condition as SelfSwitchCondition;

    public SwitchCondition? AsSwitch => Condition as SwitchCondition;

    public VariableCondition? AsVariable => Condition as VariableCondition;

    public ItemCondition? AsItem => Condition as ItemCondition;

    public ActorCondition? AsActor => Condition as ActorCondition;

    public LevelCondition? AsLevel => Condition as LevelCondition;

    public bool IsSelfSwitch => Condition is SelfSwitchCondition;

    public bool IsSwitch => Condition is SwitchCondition;

    public bool IsVariable => Condition is VariableCondition;

    public bool IsItem => Condition is ItemCondition;

    public bool IsActor => Condition is ActorCondition;

    public bool IsLevel => Condition is LevelCondition;

    #endregion

    #region Relays

    public RelayCommand ClearOwnItemCommand => new(() =>
    {
        if (AsItem is not null) AsItem.OwnItem = null;
    });

    public RelayCommand ClearDoNotOwnItemCommand => new(() =>
    {
        if (AsItem is not null) AsItem.DoNotOwnItem = null;
    });

    public RelayCommand ClearOwnActorCommand => new(() =>
    {
        if (AsActor is not null) AsActor.OwnActor = null;
    });

    public RelayCommand ClearDoNotOwnActorCommand => new(() =>
    {
        if (AsActor is not null) AsActor.DoNotOwnActor = null;
    });

    #endregion
}
