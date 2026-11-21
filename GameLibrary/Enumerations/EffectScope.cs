namespace GameLibrary.Enumerations;

public enum EffectScope
{
    Passive,
    Enemies,
    Allies,
    All,
    DeadEnemy,
    DeadAlly,
    AllNotDead,
    AllDead,
    Self, // only the fighter using it, never anyone else in the hitbox
    Party // every member of the user's party wherever they are, standing or downed (the hitbox doesn't matter)
}
