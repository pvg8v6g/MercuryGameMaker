using System.Collections.ObjectModel;
using GameLibrary.Models.Animations;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Disciplines;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.Growths;
using GameLibrary.Models.Items;
using GameLibrary.Models.States;
using GameLibrary.Models.Troops;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameLibrary.Services.GameData;

public interface IGameDataService
{
    decimal ScreenWidth { get; set; }

    decimal ScreenHeight { get; set; }

    decimal GameWidth { get; set; }

    decimal GameHeight { get; set; }

    decimal ScaleFactor { get; set; }

    int GridSize { get; set; }

    int Division { get; set; }

    decimal FrameRate { get; set; }

    int MaxEntities { get; set; }

    bool Paused { get; set; }

    string[] GameGraphicsMapFolders { get; set; }

    int LevelCap { get; set; }

    ObservableCollection<GameMap> GameMaps { get; }

    ObservableCollection<Fighter> Actors { get; }

    ObservableCollection<Discipline> Disciplines { get; }

    ObservableCollection<Arte> Artes { get; }

    ObservableCollection<Consumable> Consumables { get; }

    ObservableCollection<Equipment> Equipment { get; }

    ObservableCollection<Fighter> Enemies { get; }

    ObservableCollection<Troop> Troops { get; }

    ObservableCollection<State> States { get; }

    ObservableCollection<Animation> Animations { get; }

    ObservableCollection<Attribute> Attributes { get; }

    ObservableCollection<Attribute> Elements { get; }

    ObservableCollection<Growth> Growths { get; }
}
