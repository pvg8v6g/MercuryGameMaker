using System.Collections.ObjectModel;
using System.Text.Json;
using GameLibrary.Commands;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Conditions;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;
using GameMaker.Services.Navigation;
using GameMaker.UX.Components.EngineListView;
using GameMaker.UX.ViewModels.Popups.ConditionEditor;
using MercuryLibrary.WinUI3Components;
using ContentDialogResult = Microsoft.UI.Xaml.Controls.ContentDialogResult;

namespace GameMaker.UX.ViewModels.Popups.EnemyArteEditor;

public partial class EnemyArteEditorViewModel(
    FighterArte fighterArte,
    IGameDataService gameDataService,
    INavigationService navigationService) : PropertyChangedUpdater
{
    #region Properties

    public FighterArte FighterArte => fighterArte;

    public ObservableCollection<Arte> Artes => gameDataService.Artes;

    public ObservableCollection<Condition> Conditions => fighterArte.Conditions;

    public IReadOnlyList<EngineListNewOption> ConditionNewOptions =>
    [
        new() { Text = "Level", Command = AddConditionCommand, CommandParameter = "Level" },
        new() { Text = "Switch", Command = AddConditionCommand, CommandParameter = "Switch" },
        new() { Text = "Variable", Command = AddConditionCommand, CommandParameter = "Variable" }
    ];

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

    #endregion

    #region Relays

    public AsyncRelayCommand<string> AddConditionCommand => field ??= new AsyncRelayCommand<string>(AddCondition);

    public AsyncRelayCommand EditConditionCommand => field ??= new AsyncRelayCommand(EditCondition);

    public RelayCommand CopyConditionCommand => field ??= new RelayCommand(CopyCondition);

    public RelayCommand PasteConditionCommand => field ??= new RelayCommand(PasteCondition);

    public RelayCommand DeleteConditionCommand => field ??= new RelayCommand(DeleteCondition);

    #endregion

    #region Actions

    private async Task AddCondition(string key)
    {
        Condition? condition = key switch
        {
            "Level" => new LevelCondition(),
            "Switch" => new SwitchCondition(),
            "Variable" => new VariableCondition(),
            _ => null
        };
        if (condition is null) return;

        var result = await ShowConditionEditor(condition);
        if (result != ContentDialogResult.Primary) return;

        Conditions.Add(condition);
        SelectedConditionIndex = Conditions.Count - 1;
    }

    private async Task EditCondition()
    {
        if (SelectedConditionIndex < 0 || SelectedConditionIndex >= Conditions.Count) return;
        await ShowConditionEditor(Conditions[SelectedConditionIndex]);
    }

    private void CopyCondition()
    {
        if (SelectedConditionIndex < 0 || SelectedConditionIndex >= Conditions.Count) return;
        CopiedCondition = Conditions[SelectedConditionIndex];
    }

    private void PasteCondition()
    {
        if (CopiedCondition is null) return;

        var json = JsonSerializer.Serialize<Condition>(CopiedCondition);
        var clone = JsonSerializer.Deserialize<Condition>(json);
        if (clone is null) return;

        clone.Guid = Guid.NewGuid();
        Conditions.Add(clone);
        SelectedConditionIndex = Conditions.Count - 1;
    }

    private void DeleteCondition()
    {
        if (SelectedConditionIndex < 0 || SelectedConditionIndex >= Conditions.Count) return;
        Conditions.RemoveAt(SelectedConditionIndex);
    }

    #endregion

    #region Private Methods

    private async Task<ContentDialogResult> ShowConditionEditor(Condition condition)
    {
        var viewModel = new ConditionEditorViewModel(condition, gameDataService);
        return await navigationService.ShowConditionEditorPopup(viewModel);
    }

    #endregion
}
