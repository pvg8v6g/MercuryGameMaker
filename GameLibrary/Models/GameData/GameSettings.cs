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

    // the icons shown for Life and Mana, which aren't attributes with icons of their own
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

    #endregion
}
