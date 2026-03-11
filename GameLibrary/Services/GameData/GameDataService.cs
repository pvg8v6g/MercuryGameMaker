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
using MercuryLibrary.WinUI3Components;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameLibrary.Services.GameData;

public partial class GameDataService : PropertyChangedUpdater, IGameDataService
{
    #region Properties

    public double ScreenWidth { get; set; } = 0.0d;

    public double ScreenHeight { get; set; } = 0.0d;

    public double GameWidth { get; set; } = 0.0d;

    public double GameHeight { get; set; } = 0.0d;

    public double ScaleFactor { get; set; } = 1.0d;

    public int GridSize { get; set; } = 48;

    public int Division { get; set; } = 12;

    public double FrameRate { get; set; } = 16.6666667d; // 60fps

    public int MaxEntities { get; set; } = 5000;

    public bool Paused { get; set; }

    public string[] GameGraphicsMapFolders { get; set; } = ["Parallax", "Light", "Ground"];

    public int LevelCap { get; set; } = 99;

    public Guid LifeAttributeGuid { get; set; } = new("0581680B-6BCC-4668-AA4A-FA2BCE8160DA");

    public Guid ManaAttributeGuid { get; set; } = new("22F0A795-4295-45AB-9EEC-909164E58E91");

    #endregion

    #region Game Objects

    public ObservableCollection<GameMap> GameMaps { get; } = [];

    public ObservableCollection<Fighter> Actors { get; } = [];

    public ObservableCollection<Discipline> Disciplines { get; } = [];

    public ObservableCollection<Arte> Artes { get; } = [];

    public ObservableCollection<Consumable> Consumables { get; } = [];

    public ObservableCollection<Equipment> Equipment { get; } = [];

    public ObservableCollection<Fighter> Enemies { get; } = [];

    public ObservableCollection<Troop> Troops { get; } = [];

    public ObservableCollection<State> States { get; } = [];

    public ObservableCollection<Animation> Animations { get; } = [];

    public ObservableCollection<Attribute> Attributes { get; } = [];

    public ObservableCollection<Attribute> Elements { get; } = [];

    public ObservableCollection<Growth> Growths { get; } = [];

    #endregion
}
