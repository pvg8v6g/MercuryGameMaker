using System.Collections.ObjectModel;
using GameLibrary.Models;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Disciplines;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using GameLibrary.Models.States;
using GameLibrary.Services.GameData;
using GameMaker.UX.Models.Choices;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.BranchActionEditor;

public class BranchActionEditorViewModel(BranchAction branch, IGameDataService gameDataService, IEnumerable<Entity>? mapEntities = null) : PropertyChangedUpdater
{
    #region Properties

    public BranchAction Branch { get; } = branch;

    public ObservableCollection<Fighter> Actors => gameDataService.Actors;

    public ObservableCollection<Fighter> Enemies => gameDataService.Enemies;

    // everything the party can carry besides equipment: consumables, cooking ingredients and crafting materials
    public Item[] CarriedItems =>
    [
        .. gameDataService.Consumables.Cast<Item>()
            .Concat(gameDataService.Ingredients)
            .Concat(gameDataService.Materials)
    ];

    public ObservableCollection<Equipment> Equipment => gameDataService.Equipment;

    public ObservableCollection<Arte> Artes => gameDataService.Artes;

    public ObservableCollection<State> States => gameDataService.States;

    public ObservableCollection<Discipline> Disciplines => gameDataService.Disciplines;

    // the entity a condition looks at: this entity or another entity on the map, so nobody has to know its id
    public TargetChoice[] EntityTargets { get; } = [.. TargetChoice.List(mapEntities ?? []).Where(x => !x.Player)];

    public TargetChoice? SelfSwitchTarget
    {
        get => Find(Branch.SelfSwitchConditionEntity);
        set
        {
            if (value is null || value == SelfSwitchTarget) return;

            Branch.SelfSwitchConditionEntity = value.Id;
            OnPropertyChanged();
        }
    }

    public TargetChoice? EntityDirectionTarget
    {
        get => Find(Branch.EntityDirectionConditionEntity);
        set
        {
            if (value is null || value == EntityDirectionTarget) return;

            Branch.EntityDirectionConditionEntity = value.Id;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Private Methods

    // the picked entity, this entity when nothing is picked or the entity has since been deleted
    private TargetChoice Find(Guid? id) => EntityTargets.FirstOrDefault(x => x.Id is not null && x.Id == id) ?? EntityTargets[0];

    #endregion
}
