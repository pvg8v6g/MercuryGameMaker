using System.Collections.ObjectModel;
using System.Text.Json;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Conditions;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Json;
using GameLibrary.Utilities;
using GameLibrary.Utilities.Calculations;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.Services.Navigation;
using GameMaker.UX.Components.EngineListView;
using GameMaker.UX.ViewModels.Popups.ActionEditor;
using GameMaker.UX.ViewModels.Popups.BranchActionEditor;
using GameMaker.UX.ViewModels.Popups.ConditionEditor;
using GameMaker.UX.ViewModels.Popups.MovementRouteEditor;
using Microsoft.UI.Xaml.Media;
using ContentDialogResult = Microsoft.UI.Xaml.Controls.ContentDialogResult;

namespace GameMaker.UX.ViewModels.Popups.EntityEditor;

public class EntityEditorViewModel : BaseViewModel<Page>
{
    #region Construction

    // Accent brushes used to visually segregate the action categories in the "Add" menu,
    // mirroring the colour-coded action buttons in the Java editor. The hex values live on
    // the ActionColor enum (like GameColor) and are interpreted into brushes here.
    private static readonly Brush MessageBrush = ActionColor.Message.ToBrush();
    private static readonly Brush FlowBrush = ActionColor.Flow.ToBrush();
    private static readonly Brush PartyBrush = ActionColor.Party.ToBrush();
    private static readonly Brush ActorBrush = ActionColor.Actor.ToBrush();
    private static readonly Brush EntityBrush = ActionColor.Entity.ToBrush();
    private static readonly Brush MediaBrush = ActionColor.Media.ToBrush();
    private static readonly Brush SystemBrush = ActionColor.System.ToBrush();

    /// <summary>
    /// Returns the category colour for a page action, matching the colour-coded groups in
    /// the "Add" menu (Message=red, Flow=blue, Party=green, Actor=yellow, Entity=purple,
    /// Media/Screen=teal, System=pink). Returns null for unknown types.
    /// </summary>
    public static ActionColor? ColorForAction(PageAction action) => action switch
    {
        ShowTextAction or ShowChoicesAction or InputNumberAction or SelectItemAction => ActionColor.Message,
        FlowAction or BranchAction or ExitProcessingAction => ActionColor.Flow,
        MoneyAction or ChangeItemAction or PartyAction or RecoverAction => ActionColor.Party,
        ActorNameAction or ActorDescriptionAction or ActorImageAction or ActorAttributeAction
            or ActorLifeAction or ActorManaAction or ActorExperienceAction or ActorLevelAction
            or ActorArteAction or ActorEquipmentAction or ActorStateAction => ActionColor.Actor,
        EntityTransferAction or EntityStateAction or EntityMovementAction or EntityEraseAction
            or EntityAnimationAction or EntityBalloonIconAction or EntityOpacityAction => ActionColor.Entity,
        PlayMediaAction or FadeMediaAction or ScrollMapAction or ScreenFadeAction or WeatherAction => ActionColor.Media,
        WaitAction or MenuAction or MenuAccessAction or ShopAction or SaveAction or GameOverAction
            or InitiateBattleAction => ActionColor.System,
        _ => null
    };

    /// <summary>Resolves the category accent brush for a page action, or null for unknown types.</summary>
    public static Brush? BrushForAction(PageAction action) => ColorForAction(action)?.ToBrush();

    private readonly IGameDataService _gameDataService;
    private readonly INavigationService _navigationService;
    private bool _insertAbove;

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

    public IReadOnlyList<EngineListNewOption> ConditionNewOptions =>
    [
        new() { Text = "Self Switch", Command = AddConditionCommand, CommandParameter = "SelfSwitch" },
        new() { Text = "Switch", Command = AddConditionCommand, CommandParameter = "Switch" },
        new() { Text = "Variable", Command = AddConditionCommand, CommandParameter = "Variable" },
        new() { Text = "Item", Command = AddConditionCommand, CommandParameter = "Item" },
        new() { Text = "Actor", Command = AddConditionCommand, CommandParameter = "Actor" }
    ];

    public ObservableCollection<PageAction>? SelectedActions => SelectedPage?.Actions;

    public ObservableCollection<ActionRow> ActionRows { get; } = [];

    public IReadOnlyList<EngineListNewOption> ActionNewOptions =>
    [
        new()
        {
            Text = "Message",
            Foreground = MessageBrush,
            Children =
            [
                new() { Text = "Show Text", Command = AddActionCommand, CommandParameter = "ShowText" },
                new() { Text = "Show Choices", Command = AddActionCommand, CommandParameter = "ShowChoices" },
                new() { Text = "Input Number", Command = AddActionCommand, CommandParameter = "InputNumber" },
                new() { Text = "Select Item", Command = AddActionCommand, CommandParameter = "SelectItem" }
            ]
        },
        new()
        {
            Text = "Flow",
            Foreground = FlowBrush,
            Children =
            [
                new() { Text = "Condition Control", Command = AddActionCommand, CommandParameter = "Flow" },
                new() { Text = "Branch Flow", Command = AddActionCommand, CommandParameter = "Branch" },
                new() { Text = "Exit Page Actions", Command = AddActionCommand, CommandParameter = "ExitProcessing" }
            ]
        },
        new()
        {
            Text = "Party",
            Foreground = PartyBrush,
            Children =
            [
                new() { Text = "Change Money", Command = AddActionCommand, CommandParameter = "Money" },
                new() { Text = "Change Items", Command = AddActionCommand, CommandParameter = "ChangeItem" },
                new() { Text = "Party", Command = AddActionCommand, CommandParameter = "Party" },
                new() { Text = "Recover All", Command = AddActionCommand, CommandParameter = "Recover" }
            ]
        },
        new()
        {
            Text = "Actor",
            Foreground = ActorBrush,
            Children =
            [
                new() { Text = "Actor Name", Command = AddActionCommand, CommandParameter = "ActorName" },
                new() { Text = "Actor Description", Command = AddActionCommand, CommandParameter = "ActorDescription" },
                new() { Text = "Actor Image", Command = AddActionCommand, CommandParameter = "ActorImage" },
                new() { Text = "Actor Attribute", Command = AddActionCommand, CommandParameter = "ActorAttribute" },
                new() { Text = "Actor Life", Command = AddActionCommand, CommandParameter = "ActorLife" },
                new() { Text = "Actor Mana", Command = AddActionCommand, CommandParameter = "ActorMana" },
                new() { Text = "Actor Experience", Command = AddActionCommand, CommandParameter = "ActorExperience" },
                new() { Text = "Actor Level", Command = AddActionCommand, CommandParameter = "ActorLevel" },
                new() { Text = "Actor Arte", Command = AddActionCommand, CommandParameter = "ActorArte" },
                new() { Text = "Actor Equipment", Command = AddActionCommand, CommandParameter = "ActorEquipment" },
                new() { Text = "Actor States", Command = AddActionCommand, CommandParameter = "ActorState" }
            ]
        },
        new()
        {
            Text = "Entity",
            Foreground = EntityBrush,
            Children =
            [
                new() { Text = "Entity Transfer", Command = AddActionCommand, CommandParameter = "EntityTransfer" },
                new() { Text = "Entity States", Command = AddActionCommand, CommandParameter = "EntityState" },
                new() { Text = "Entity Movement", Command = AddMovementRouteCommand },
                new() { Text = "Entity Erase", Command = AddActionCommand, CommandParameter = "EntityErase" },
                new() { Text = "Entity Animation", Command = AddActionCommand, CommandParameter = "EntityAnimation" },
                new() { Text = "Entity Balloon Icon", Command = AddActionCommand, CommandParameter = "EntityBalloonIcon" },
                new() { Text = "Entity Opacity", Command = AddActionCommand, CommandParameter = "EntityOpacity" }
            ]
        },
        new()
        {
            Text = "Media / Screen",
            Foreground = MediaBrush,
            Children =
            [
                new() { Text = "Play Media", Command = AddActionCommand, CommandParameter = "PlayMedia" },
                new() { Text = "Fade Media", Command = AddActionCommand, CommandParameter = "FadeMedia" },
                new() { Text = "Scroll Map", Command = AddActionCommand, CommandParameter = "ScrollMap" },
                new() { Text = "Screen Fade", Command = AddActionCommand, CommandParameter = "ScreenFade" },
                new() { Text = "Weather", Command = AddActionCommand, CommandParameter = "Weather" }
            ]
        },
        new()
        {
            Text = "System",
            Foreground = SystemBrush,
            Children =
            [
                new() { Text = "Wait", Command = AddActionCommand, CommandParameter = "Wait" },
                new() { Text = "Open Menu", Command = AddActionCommand, CommandParameter = "Menu" },
                new() { Text = "Menu Access", Command = AddActionCommand, CommandParameter = "MenuAccess" },
                new() { Text = "Open Shop", Command = AddActionCommand, CommandParameter = "Shop" },
                new() { Text = "Save Game", Command = AddActionCommand, CommandParameter = "Save" },
                new() { Text = "Game Over", Command = AddActionCommand, CommandParameter = "GameOver" },
                new() { Text = "Start Battle", Command = AddActionCommand, CommandParameter = "InitiateBattle" }
            ]
        }
    ];

    public PageAction? CopiedAction
    {
        get;
        private set => SetField(ref field, value);
    }

    public int SelectedConditionIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    public Condition? CopiedCondition
    {
        get;
        private set => SetField(ref field, value);
    }

    public int SelectedActionIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    public int SelectedActionRowIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    protected override ObservableCollection<Page> EntityCollection => Entity.Pages;

    #endregion

    #region Relays

    public AsyncRelayCommand<string> AddConditionCommand => new(AddCondition);

    public AsyncRelayCommand EditConditionCommand => new(EditCondition);

    public RelayCommand CopyConditionCommand => new(CopyCondition);

    public RelayCommand PasteConditionCommand => new(PasteCondition);

    public RelayCommand DeleteConditionCommand => new(DeleteCondition);

    public RelayCommand DeleteActionCommand => new(DeleteAction);

    public RelayCommand CopyActionCommand => new(CopyAction);

    public RelayCommand PasteActionCommand => new(PasteAction);

    public AsyncRelayCommand AddMovementRouteCommand => new(AddMovementRoute);

    public AsyncRelayCommand<string> AddActionCommand => new(AddAction);

    public AsyncRelayCommand EditActionCommand => new(EditAction);

    public RelayCommand DoubleTapActionCommand => new(DoubleTapAction);

    #endregion

    #region Actions

    protected override void NewEntity(string? newEntityName = null)
    {
        newEntityName ??= "New Page";
        var newObject = new Page
        {
            Id = Calculations.GetNextId([.. EntityCollection.Select(x => x.Id)]),
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
        SelectedConditionIndex = -1;
        SelectedActionIndex = -1;
        RebuildActionRows();
        return Task.CompletedTask;
    }

    private async Task AddCondition(string key)
    {
        if (SelectedConditions is null) return;

        var condition = CreateCondition(key);
        if (condition is null) return;

        var result = await ShowConditionEditor(condition);
        if (result != ContentDialogResult.Primary) return;

        SelectedConditions.Add(condition);
        SelectedConditionIndex = SelectedConditions.Count - 1;
    }

    private async Task EditCondition()
    {
        if (SelectedConditions is null) return;
        if (SelectedConditionIndex < 0 || SelectedConditionIndex >= SelectedConditions.Count) return;

        await ShowConditionEditor(SelectedConditions[SelectedConditionIndex]);
    }

    private void CopyCondition()
    {
        if (SelectedConditions is null) return;
        if (SelectedConditionIndex < 0 || SelectedConditionIndex >= SelectedConditions.Count) return;
        CopiedCondition = SelectedConditions[SelectedConditionIndex];
    }

    private void PasteCondition()
    {
        if (SelectedConditions is null || CopiedCondition is null) return;

        var json = JsonSerializer.Serialize<Condition>(CopiedCondition);
        var clone = JsonSerializer.Deserialize<Condition>(json);
        if (clone is null) return;

        clone.Guid = Guid.NewGuid();
        SelectedConditions.Add(clone);
        SelectedConditionIndex = SelectedConditions.Count - 1;
    }

    private void DeleteCondition()
    {
        if (SelectedConditions is null) return;
        if (SelectedConditionIndex < 0 || SelectedConditionIndex >= SelectedConditions.Count) return;
        SelectedConditions.RemoveAt(SelectedConditionIndex);
    }

    private async Task<ContentDialogResult> ShowConditionEditor(Condition condition)
    {
        var viewModel = new ConditionEditorViewModel(condition, _gameDataService);
        return await _navigationService.ShowConditionEditorPopup(viewModel);
    }

    private static Condition? CreateCondition(string key) => key switch
    {
        "SelfSwitch" => new SelfSwitchCondition(),
        "Switch" => new SwitchCondition(),
        "Variable" => new VariableCondition(),
        "Item" => new ItemCondition(),
        "Actor" => new ActorCondition(),
        _ => null
    };

    private void DoubleTapAction()
    {
        // A double-tap requests inserting the next created action above the tapped row.
        _insertAbove = true;
    }

    private void DeleteAction()
    {
        _insertAbove = false;
        var row = SelectedRow();
        if (row?.Action is null) return;

        var target = CollectionFor(row);
        target.Remove(row.Action);
        RebuildActionRows();
    }

    private void CopyAction()
    {
        _insertAbove = false;
        var row = SelectedRow();
        if (row?.Action is null) return;
        CopiedAction = row.Action;
    }

    private void PasteAction()
    {
        if (SelectedPage is null || CopiedAction is null) return;

        var json = JsonSerializer.Serialize(CopiedAction);
        var clone = JsonSerializer.Deserialize<PageAction>(json);
        if (clone is null) return;

        ReassignGuids(clone);
        InsertAction(clone);
    }

    private async Task AddAction(string key)
    {
        if (SelectedPage is null) return;

        var action = CreateAction(key);
        if (action is null) return;

        var result = await ShowEditorFor(action);
        if (result != ContentDialogResult.Primary) return;

        InsertAction(action);
    }

    private static PageAction? CreateAction(string key) => key switch
    {
        "ShowText" => new ShowTextAction(),
        "ShowChoices" => new ShowChoicesAction(),
        "InputNumber" => new InputNumberAction(),
        "SelectItem" => new SelectItemAction(),
        "Flow" => new FlowAction(),
        "Branch" => new BranchAction(),
        "ExitProcessing" => new ExitProcessingAction(),
        "Wait" => new WaitAction(),
        "Money" => new MoneyAction(),
        "ChangeItem" => new ChangeItemAction(),
        "Party" => new PartyAction(),
        "Recover" => new RecoverAction(),
        "ActorName" => new ActorNameAction(),
        "ActorDescription" => new ActorDescriptionAction(),
        "ActorImage" => new ActorImageAction(),
        "ActorLife" => new ActorLifeAction(),
        "ActorMana" => new ActorManaAction(),
        "ActorExperience" => new ActorExperienceAction(),
        "ActorLevel" => new ActorLevelAction(),
        "ActorAttribute" => new ActorAttributeAction(),
        "ActorArte" => new ActorArteAction(),
        "ActorEquipment" => new ActorEquipmentAction(),
        "ActorState" => new ActorStateAction(),
        "EntityTransfer" => new EntityTransferAction(),
        "EntityState" => new EntityStateAction(),
        "EntityErase" => new EntityEraseAction(),
        "EntityAnimation" => new EntityAnimationAction(),
        "EntityBalloonIcon" => new EntityBalloonIconAction(),
        "EntityOpacity" => new EntityOpacityAction(),
        "PlayMedia" => new PlayMediaAction(),
        "FadeMedia" => new FadeMediaAction(),
        "ScrollMap" => new ScrollMapAction(),
        "ScreenFade" => new ScreenFadeAction(),
        "Weather" => new WeatherAction(),
        "Menu" => new MenuAction(),
        "MenuAccess" => new MenuAccessAction(),
        "Shop" => new ShopAction(),
        "Save" => new SaveAction(),
        "GameOver" => new GameOverAction(),
        "InitiateBattle" => new InitiateBattleAction(),
        _ => null
    };

    private async Task<ContentDialogResult> ShowEditorFor(PageAction action)
    {
        switch (action)
        {
            case GameOverAction:
            case ExitProcessingAction:
                return ContentDialogResult.Primary;
            case BranchAction branch:
                return await ShowBranchEditor(branch);
            case EntityMovementAction movement:
                await ShowMovementRoute(movement);
                return ContentDialogResult.Primary;
            default:
                return await ShowActionEditor(action);
        }
    }

    private async Task<ContentDialogResult> ShowActionEditor(PageAction action)
    {
        var viewModel = new ActionEditorViewModel(action, _gameDataService);
        return await _navigationService.ShowActionEditorPopup(viewModel);
    }

    private async Task<ContentDialogResult> ShowBranchEditor(BranchAction branch)
    {
        var viewModel = new BranchActionEditorViewModel(branch, _gameDataService);
        return await _navigationService.ShowBranchActionEditorPopup(viewModel);
    }

    private async Task AddMovementRoute()
    {
        if (SelectedPage is null) return;

        var action = new EntityMovementAction();
        InsertAction(action);

        await ShowMovementRoute(action);
    }

    private async Task EditAction()
    {
        _insertAbove = false;
        var row = SelectedRow();
        if (row?.Action is null) return;

        await ShowEditorFor(row.Action);
        RebuildActionRows();
        SelectActionRow(row.Action);
    }

    private async Task ShowMovementRoute(EntityMovementAction action)
    {
        var viewModel = new MovementRouteEditorViewModel(action, _gameDataService, _navigationService);
        await _navigationService.ShowMovementRouteEditorPopup(viewModel);
    }

    #endregion

    #region Action rows

    private ActionRow? SelectedRow()
    {
        if (SelectedActionRowIndex < 0 || SelectedActionRowIndex >= ActionRows.Count) return null;
        return ActionRows[SelectedActionRowIndex];
    }

    private ObservableCollection<PageAction> CollectionFor(ActionRow row)
    {
        if (row.ParentBranch is null) return SelectedPage!.Actions;
        return row.InElse ? row.ParentBranch.ElseActions : row.ParentBranch.BranchActions;
    }

    private void InsertAction(PageAction action)
    {
        if (SelectedPage is null) return;

        var row = SelectedRow();
        ObservableCollection<PageAction> target;
        int index;

        if (row is null || row.IsMarker)
        {
            target = SelectedPage.Actions;
            index = target.Count;
        }
        else
        {
            target = CollectionFor(row);
            if (row.Kind == ActionRowKind.Blank || row.Action is null)
            {
                index = target.Count;
            }
            else
            {
                var existing = target.IndexOf(row.Action);
                if (existing < 0)
                {
                    index = target.Count;
                }
                else
                {
                    index = _insertAbove ? existing : existing + 1;
                }
            }
        }

        _insertAbove = false;
        target.Insert(index, action);
        RebuildActionRows();
        SelectActionRow(action);
    }

    private void SelectActionRow(PageAction action)
    {
        for (var i = 0; i < ActionRows.Count; i++)
        {
            if (ActionRows[i].Action == action)
            {
                SelectedActionRowIndex = i;
                return;
            }
        }
    }

    private void RebuildActionRows()
    {
        ActionRows.Clear();
        if (SelectedPage is null) return;

        BuildInto(SelectedPage.Actions, null, false, 0);
        ActionRows.Add(new ActionRow { Kind = ActionRowKind.Blank, Indent = 0 });
    }

    private void BuildInto(ObservableCollection<PageAction> list, BranchAction? parent, bool inElse, int indent)
    {
        foreach (var action in list)
        {
            ActionRows.Add(new ActionRow
            {
                Action = action,
                Kind = ActionRowKind.Normal,
                Indent = indent,
                ParentBranch = parent,
                InElse = inElse
            });

            if (action is not BranchAction branch) continue;

            BuildInto(branch.BranchActions, branch, false, indent + 1);
            ActionRows.Add(new ActionRow { Kind = ActionRowKind.Blank, Indent = indent + 1, ParentBranch = branch });

            if (branch.ElseBranch)
            {
                ActionRows.Add(new ActionRow { Kind = ActionRowKind.Else, Indent = indent, ParentBranch = branch });
                BuildInto(branch.ElseActions, branch, true, indent + 1);
                ActionRows.Add(new ActionRow { Kind = ActionRowKind.Blank, Indent = indent + 1, ParentBranch = branch, InElse = true });
            }

            ActionRows.Add(new ActionRow { Kind = ActionRowKind.End, Indent = indent, ParentBranch = branch });
        }
    }

    private static void ReassignGuids(PageAction action)
    {
        action.Guid = Guid.NewGuid();
        if (action is not BranchAction branch) return;

        foreach (var child in branch.BranchActions) ReassignGuids(child);
        foreach (var child in branch.ElseActions) ReassignGuids(child);
    }

    #endregion
}
