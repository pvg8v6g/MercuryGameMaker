using GameLibrary.Enumerations;
using GameLibrary.Models.Media;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.GameData;

/// <summary>
/// Rules that apply to the whole game, set per game in the editor.
/// Saved on its own in Settings\GameSettings.data.
/// </summary>
public partial class GameSettings : PropertyChangedUpdater
{
    #region Constants

    public const int DefaultLevelCap = 99;

    #endregion

    #region Properties

    // the highest level an actor can reach, growths are made for levels 1 to this
    public int LevelCap
    {
        get;
        set => SetField(ref field, Math.Max(1, value));
    } = DefaultLevelCap;

    // total experience to reach a level is ExperienceBase * (level - 1) ^ ExperienceExponent: with 20 and 2, level 2 takes 20
    // and level 10 takes 1620. Experience is a running total on the actor, it isn't spent on levelling
    public int ExperienceBase
    {
        get;
        set => SetField(ref field, Math.Max(1, value));
    } = 20;

    public double ExperienceExponent
    {
        get;
        set => SetField(ref field, Math.Max(1, value));
    } = 2;

    public int ExperienceForLevel(int level) => level <= 1 ? 0 : (int) Math.Round(ExperienceBase * Math.Pow(level - 1, ExperienceExponent));

    // the volume and pitch a new sound starts with (a sound effect, music track or the like that nothing was set on). The
    // sounds already in the game keep their own. Copied to AudioDefaults, which the models read
    public double DefaultVolume
    {
        get;
        set
        {
            SetField(ref field, Math.Clamp(value, 0.0, 1.0));
            AudioDefaults.Volume = field;
        }
    } = AudioDefaults.StandardVolume;

    public double DefaultPitch
    {
        get;
        set
        {
            SetField(ref field, Math.Clamp(value, 0.1, 4.0));
            AudioDefaults.Pitch = field;
        }
    } = AudioDefaults.StandardPitch;

    // the short sound effect (a file in Media\SFX) played when the victory screen opens, none when there isn't one. The screen
    // goes away when it's over
    public AudioFile? VictorySfx
    {
        get;
        set => SetField(ref field, value);
    }

    // what losing a battle does once the Game Over screen is closed: the game is over, or the battle is fought again
    public DefeatOutcome Defeat
    {
        get;
        set => SetField(ref field, value);
    } = DefeatOutcome.GameOver;

    // the short sound effect (a file in Media\SFX) played when the Game Over screen opens, none when there isn't one
    public AudioFile? DefeatSfx
    {
        get;
        set => SetField(ref field, value);
    }

    // the icons shown for Life and Mana, which aren't attributes with icons of their own (set on the Terms page)
    public GameIcon LifeIcon
    {
        get;
        set => SetField(ref field, value ?? GameIcon.None);
    } = GameIcon.None;

    public GameIcon ManaIcon
    {
        get;
        set => SetField(ref field, value ?? GameIcon.None);
    } = GameIcon.None;

    // the icon for money, shown on money an enemy drops
    public GameIcon MoneyIcon
    {
        get;
        set => SetField(ref field, value ?? GameIcon.None);
    } = GameIcon.None;

    // what the game calls Life, Mana and money, so a game can say HP, MP or Magic, and Gold or Crowns
    public string LifeName
    {
        get;
        set => SetField(ref field, string.IsNullOrWhiteSpace(value) ? "Life" : value);
    } = "Life";

    public string ManaName
    {
        get;
        set => SetField(ref field, string.IsNullOrWhiteSpace(value) ? "Mana" : value);
    } = "Mana";

    public string MoneyName
    {
        get;
        set => SetField(ref field, string.IsNullOrWhiteSpace(value) ? "Money" : value);
    } = "Money";

    #endregion
}
