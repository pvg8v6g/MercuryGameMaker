using GameLibrary.Enumerations;

namespace GameLibrary.Models.Movements;

public partial class CharacterImageMovement : Movement
{
    public string? CharacterName
    {
        get;
        set => SetField(ref field, value);
    }

    public int CharacterIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public Direction Direction
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;

    public override string ToString() => $"Set Image: {CharacterName}";
}
