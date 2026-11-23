namespace GameLibrary.Models.Movements;

/// <summary>
/// Puts the entity, feet first, on a random open tile of the area it stands in: every tile it could walk to without
/// crossing a collision area on its level (in a battle, the field inside the walls). Entities placed this way don't
/// share a tile while there are free ones, so a shower of meteors spreads out across the field.
/// </summary>
public partial class PlaceRandomlyMovement : Movement
{
    public override string ToString() => "Place Randomly";
}
