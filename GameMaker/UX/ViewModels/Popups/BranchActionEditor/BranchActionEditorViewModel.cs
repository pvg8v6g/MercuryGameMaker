using System.Collections.ObjectModel;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Disciplines;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using GameLibrary.Models.States;
using GameLibrary.Services.GameData;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.BranchActionEditor;

public class BranchActionEditorViewModel(BranchAction branch, IGameDataService gameDataService) : PropertyChangedUpdater
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

    #endregion
}
