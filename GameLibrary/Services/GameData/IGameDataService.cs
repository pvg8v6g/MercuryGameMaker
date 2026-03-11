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
    double ScreenWidth { get; set; }

    double ScreenHeight { get; set; }

    double GameWidth { get; set; }

    double GameHeight { get; set; }

    double ScaleFactor { get; set; }

    int GridSize { get; set; }

    int Division { get; set; }

    double FrameRate { get; set; }

    int MaxEntities { get; set; }

    bool Paused { get; set; }

    string[] GameGraphicsMapFolders { get; set; }

    int LevelCap { get; set; }

    public Guid LifeAttributeGuid { get; set; }

    public Guid ManaAttributeGuid { get; set; }

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
