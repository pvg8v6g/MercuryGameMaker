namespace GameEngine.Interface;

/// <summary>
/// One party member's row on the victory screen: the face, the name, the level (and what it was if it went up) and the
/// EXP they got. Made on the ui thread and never changed.
/// </summary>
public class VictoryMember(HudFighter fighter, string levelText, string experienceText, bool leveledUp)
{
    // only used for its face
    public HudFighter Fighter => fighter;

    public string Name => fighter.Name;

    public string LevelText => levelText;

    public string ExperienceText => experienceText;

    public bool LeveledUp => leveledUp;
}
