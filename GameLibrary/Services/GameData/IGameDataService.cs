using GameLibrary.Models.Props;
using System.Collections.ObjectModel;
using GameLibrary.Models.Animations;
using GameLibrary.Models.Artes;
using GameLibrary.Models.Balloons;
using GameLibrary.Models.Disciplines;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.GameMaps;
using GameLibrary.Models.GameData;
using GameLibrary.Models.Growths;
using GameLibrary.Models.Items;
using GameLibrary.Models.States;
using GameLibrary.Models.Troops;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameLibrary.Services.GameData;

public interface IGameDataService
{
    double WindowWidth { get; set; }

    double WindowHeight { get; set; }

    double ScreenWidth { get; set; }

    double ScreenHeight { get; set; }

    double GameWidth { get; set; }

    double GameHeight { get; set; }

    double ScaleFactor { get; set; }

    int GridSize { get; set; }

    int Division { get; set; }

    int StepSize { get; }

    double FrameRate { get; set; }

    int MaxEntities { get; set; }

    bool Paused { get; set; }

    string[] GameGraphicsMapFolders { get; set; }

    int LevelCap { get; set; }

    int ExperienceBase { get; set; }

    double ExperienceExponent { get; set; }

    // the total experience an actor needs to be this level
    int ExperienceForLevel(int level);

    public Guid LifeAttributeGuid { get; }

    public Guid ManaAttributeGuid { get; }

    ObservableCollection<GameMap> GameMaps { get; }

    // where a new game begins and who is in the party
    GameStart GameStart { get; set; }

    // rules for the whole game, such as the level cap
    GameSettings GameSettings { get; set; }

    ObservableCollection<Fighter> Actors { get; }

    ObservableCollection<Discipline> Disciplines { get; }

    ObservableCollection<Arte> Artes { get; }

    ObservableCollection<Consumable> Consumables { get; }

    // cooking ingredients
    ObservableCollection<Ingredient> Ingredients { get; }

    // crafting materials and valuables
    ObservableCollection<Material> Materials { get; }

    ObservableCollection<Equipment> Equipment { get; }

    ObservableCollection<Fighter> Enemies { get; }

    ObservableCollection<Troop> Troops { get; }

    ObservableCollection<State> States { get; }

    ObservableCollection<Animation> Animations { get; }

    ObservableCollection<Prop> Props { get; }

    ObservableCollection<FillSet> FillSets { get; }

    ObservableCollection<Attribute> Attributes { get; }

    ObservableCollection<Attribute> Elements { get; }

    ObservableCollection<Growth> Growths { get; }

    ObservableCollection<BalloonIcon> BalloonIcons { get; }
}
