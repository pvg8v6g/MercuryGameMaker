namespace GameLibrary.Enumerations;

// who a projectile detonates on when it touches them, counted from the side of the fighter who fired it
public enum ContactTarget
{
    None, // never on contact, only on its Detonate step
    Enemies,
    Allies,
    Anyone
}
