using System.Collections.ObjectModel;
using System.Linq;
using GameLibrary.Commands;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Animations;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Items;
using GameLibrary.Models.States;
using GameLibrary.Models.Troops;
using GameLibrary.Services.GameData;
using MercuryLibrary.WinUI3Components;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.ViewModels.Popups.ActionEditor;

public class ActionEditorViewModel(PageAction action, IGameDataService gameDataService) : PropertyChangedUpdater
{
    #region Properties

    public PageAction Action { get; } = action;

    public ObservableCollection<Fighter> Actors => gameDataService.Actors;

    public ObservableCollection<Fighter> Enemies => gameDataService.Enemies;

    public ObservableCollection<Consumable> Consumables => gameDataService.Consumables;

    public ObservableCollection<Equipment> Equipment => gameDataService.Equipment;

    public ObservableCollection<Arte> Artes => gameDataService.Artes;

    public ObservableCollection<Attribute> Attributes => gameDataService.Attributes;

    public ObservableCollection<State> States => gameDataService.States;

    public ObservableCollection<Animation> Animations => gameDataService.Animations;

    public ObservableCollection<Troop> Troops => gameDataService.Troops;

    // folders only organize the map tree, entities can only be transferred to real maps
    public GameMap[] GameMaps => [.. gameDataService.GameMaps.Where(x => !x.FolderOnly).OrderBy(x => x.Id)];

    #endregion

    #region Typed accessors

    public ShowTextAction? AsShowText => Action as ShowTextAction;

    public WaitAction? AsWait => Action as WaitAction;

    public MoneyAction? AsMoney => Action as MoneyAction;

    public ChangeItemAction? AsChangeItem => Action as ChangeItemAction;

    public ActorNameAction? AsActorName => Action as ActorNameAction;

    public ActorDescriptionAction? AsActorDescription => Action as ActorDescriptionAction;

    public ActorLifeAction? AsActorLife => Action as ActorLifeAction;

    public ActorManaAction? AsActorMana => Action as ActorManaAction;

    public ActorExperienceAction? AsActorExperience => Action as ActorExperienceAction;

    public ActorLevelAction? AsActorLevel => Action as ActorLevelAction;

    public ActorAttributeAction? AsActorAttribute => Action as ActorAttributeAction;

    public ActorArteAction? AsActorArte => Action as ActorArteAction;

    public ActorEquipmentAction? AsActorEquipment => Action as ActorEquipmentAction;

    public ActorImageAction? AsActorImage => Action as ActorImageAction;

    public ActorStateAction? AsActorState => Action as ActorStateAction;

    public EntityAnimationAction? AsEntityAnimation => Action as EntityAnimationAction;

    public EntityBalloonIconAction? AsEntityBalloonIcon => Action as EntityBalloonIconAction;

    public EntityEraseAction? AsEntityErase => Action as EntityEraseAction;

    public EntityOpacityAction? AsEntityOpacity => Action as EntityOpacityAction;

    public EntityStateAction? AsEntityState => Action as EntityStateAction;

    public EntityTransferAction? AsEntityTransfer => Action as EntityTransferAction;

    public FadeMediaAction? AsFadeMedia => Action as FadeMediaAction;

    public FlowAction? AsFlow => Action as FlowAction;

    public InitiateBattleAction? AsInitiateBattle => Action as InitiateBattleAction;

    public InputNumberAction? AsInputNumber => Action as InputNumberAction;

    public MenuAccessAction? AsMenuAccess => Action as MenuAccessAction;

    public MenuAction? AsMenu => Action as MenuAction;

    public PartyAction? AsParty => Action as PartyAction;

    public PlayMediaAction? AsPlayMedia => Action as PlayMediaAction;

    public RecoverAction? AsRecover => Action as RecoverAction;

    public SaveAction? AsSave => Action as SaveAction;

    public ScreenFadeAction? AsScreenFade => Action as ScreenFadeAction;

    public ScrollMapAction? AsScrollMap => Action as ScrollMapAction;

    public SelectItemAction? AsSelectItem => Action as SelectItemAction;

    public ShopAction? AsShop => Action as ShopAction;

    public ShowChoicesAction? AsShowChoices => Action as ShowChoicesAction;

    #endregion

    #region Type checks

    public bool IsShowText => Action is ShowTextAction;

    public bool IsWait => Action is WaitAction;

    public bool IsMoney => Action is MoneyAction;

    public bool IsChangeItem => Action is ChangeItemAction;

    public bool IsActorName => Action is ActorNameAction;

    public bool IsActorDescription => Action is ActorDescriptionAction;

    public bool IsActorLife => Action is ActorLifeAction;

    public bool IsActorMana => Action is ActorManaAction;

    public bool IsActorExperience => Action is ActorExperienceAction;

    public bool IsActorLevel => Action is ActorLevelAction;

    public bool IsActorAttribute => Action is ActorAttributeAction;

    public bool IsActorArte => Action is ActorArteAction;

    public bool IsActorEquipment => Action is ActorEquipmentAction;

    public bool IsActorImage => Action is ActorImageAction;

    public bool IsActorState => Action is ActorStateAction;

    public bool IsEntityAnimation => Action is EntityAnimationAction;

    public bool IsEntityBalloonIcon => Action is EntityBalloonIconAction;

    public bool IsEntityErase => Action is EntityEraseAction;

    public bool IsEntityOpacity => Action is EntityOpacityAction;

    public bool IsEntityState => Action is EntityStateAction;

    public bool IsEntityTransfer => Action is EntityTransferAction;

    public bool IsExitProcessing => Action is ExitProcessingAction;

    public bool IsFadeMedia => Action is FadeMediaAction;

    public bool IsFlow => Action is FlowAction;

    public bool IsGameOver => Action is GameOverAction;

    public bool IsInitiateBattle => Action is InitiateBattleAction;

    public bool IsInputNumber => Action is InputNumberAction;

    public bool IsMenuAccess => Action is MenuAccessAction;

    public bool IsMenu => Action is MenuAction;

    public bool IsParty => Action is PartyAction;

    public bool IsPlayMedia => Action is PlayMediaAction;

    public bool IsRecover => Action is RecoverAction;

    public bool IsSave => Action is SaveAction;

    public bool IsScreenFade => Action is ScreenFadeAction;

    public bool IsScrollMap => Action is ScrollMapAction;

    public bool IsSelectItem => Action is SelectItemAction;

    public bool IsShop => Action is ShopAction;

    public bool IsShowChoices => Action is ShowChoicesAction;

    public bool IsWeather => Action is WeatherAction;

    #endregion

    #region Synced selections

    public Guid? ActorAttributeActorId
    {
        get => AsActorAttribute?.ActorId;
        set
        {
            if (AsActorAttribute is null) return;
            AsActorAttribute.ActorId = value;
            AsActorAttribute.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorAttributeAttributeId
    {
        get => AsActorAttribute?.AttributeId;
        set
        {
            if (AsActorAttribute is null) return;
            AsActorAttribute.AttributeId = value;
            AsActorAttribute.AttributeName = Attributes.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorArteActorId
    {
        get => AsActorArte?.ActorId;
        set
        {
            if (AsActorArte is null) return;
            AsActorArte.ActorId = value;
            AsActorArte.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorArteArteId
    {
        get => AsActorArte?.ArteId;
        set
        {
            if (AsActorArte is null) return;
            AsActorArte.ArteId = value;
            AsActorArte.ArteName = Artes.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ChangeItemItemId
    {
        get => AsChangeItem?.ItemId;
        set
        {
            if (AsChangeItem is null) return;
            AsChangeItem.ItemId = value;
            AsChangeItem.ItemName = Consumables.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorDescriptionActorId
    {
        get => AsActorDescription?.ActorId;
        set
        {
            if (AsActorDescription is null) return;
            AsActorDescription.ActorId = value;
            AsActorDescription.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorLifeActorId
    {
        get => AsActorLife?.ActorId;
        set
        {
            if (AsActorLife is null) return;
            AsActorLife.ActorId = value;
            AsActorLife.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorManaActorId
    {
        get => AsActorMana?.ActorId;
        set
        {
            if (AsActorMana is null) return;
            AsActorMana.ActorId = value;
            AsActorMana.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorExperienceActorId
    {
        get => AsActorExperience?.ActorId;
        set
        {
            if (AsActorExperience is null) return;
            AsActorExperience.ActorId = value;
            AsActorExperience.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorLevelActorId
    {
        get => AsActorLevel?.ActorId;
        set
        {
            if (AsActorLevel is null) return;
            AsActorLevel.ActorId = value;
            AsActorLevel.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorEquipmentActorId
    {
        get => AsActorEquipment?.ActorId;
        set
        {
            if (AsActorEquipment is null) return;
            AsActorEquipment.ActorId = value;
            AsActorEquipment.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorEquipmentEquipmentId
    {
        get => AsActorEquipment?.EquipmentId;
        set
        {
            if (AsActorEquipment is null) return;
            AsActorEquipment.EquipmentId = value;
            AsActorEquipment.EquipmentName = Equipment.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorImageActorId
    {
        get => AsActorImage?.ActorId;
        set
        {
            if (AsActorImage is null) return;
            AsActorImage.ActorId = value;
            AsActorImage.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorStateActorId
    {
        get => AsActorState?.ActorId;
        set
        {
            if (AsActorState is null) return;
            AsActorState.ActorId = value;
            AsActorState.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? ActorStateStateId
    {
        get => AsActorState?.StateId;
        set
        {
            if (AsActorState is null) return;
            AsActorState.StateId = value;
            AsActorState.StateName = States.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? EntityAnimationAnimationId
    {
        get => AsEntityAnimation?.AnimationId;
        set
        {
            if (AsEntityAnimation is null) return;
            AsEntityAnimation.AnimationId = value;
            AsEntityAnimation.AnimationName = Animations.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? EntityStateStateId
    {
        get => AsEntityState?.StateId;
        set
        {
            if (AsEntityState is null) return;
            AsEntityState.StateId = value;
            AsEntityState.StateName = States.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? EntityTransferMapId
    {
        get => AsEntityTransfer?.MapId;
        set
        {
            if (AsEntityTransfer is null) return;
            AsEntityTransfer.MapId = value;
            AsEntityTransfer.MapName = GameMaps.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? PartyActorId
    {
        get => AsParty?.ActorId;
        set
        {
            if (AsParty is null) return;
            AsParty.ActorId = value;
            AsParty.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? RecoverActorId
    {
        get => AsRecover?.ActorId;
        set
        {
            if (AsRecover is null) return;
            AsRecover.ActorId = value;
            AsRecover.ActorName = Actors.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? InitiateBattleTroopId
    {
        get => AsInitiateBattle?.TroopId;
        set
        {
            if (AsInitiateBattle is null) return;
            AsInitiateBattle.TroopId = value;
            AsInitiateBattle.TroopName = Troops.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    public Guid? SaveAnimationId
    {
        get => AsSave?.AnimationId;
        set
        {
            if (AsSave is null) return;
            AsSave.AnimationId = value;
            AsSave.AnimationName = Animations.FirstOrDefault(x => x.Guid == value)?.Name;
        }
    }

    #endregion

    #region Choices

    public Choice? SelectedChoice
    {
        get;
        set => SetField(ref field, value);
    }

    public RelayCommand AddChoiceCommand => new(() =>
    {
        if (AsShowChoices is null) return;
        Choice choice = new() { Text = "New Choice" };
        AsShowChoices.Choices.Add(choice);
        SelectedChoice = choice;
    });

    public RelayCommand RemoveChoiceCommand => new(() =>
    {
        if (AsShowChoices is null || SelectedChoice is null) return;
        AsShowChoices.Choices.Remove(SelectedChoice);
        SelectedChoice = null;
    });

    #endregion

    #region Relays

    public RelayCommand ClearItemCommand => new(() =>
    {
        if (AsChangeItem is null) return;
        AsChangeItem.ItemId = null;
        AsChangeItem.ItemName = null;
    });

    #endregion
}
