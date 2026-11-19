using GameLibrary.Models;
using GameLibrary.Models.Movements;

namespace GameMaker.UX.Models.Choices;

/// <summary>
/// Who a step acts on: the entity it belongs to (no id), the player, or another entity on the map by its guid.
/// MapId is the entity's id on the map, what toward and away steps save.
/// </summary>
public record TargetChoice(Guid? Id, bool Player, string Name, int? MapId = null)
{
    public const string ThisEntity = "This Entity";

    public override string ToString() => Name;

    // this entity, the player, then the map's entities as "id: name" in id order
    public static TargetChoice[] List(IEnumerable<Entity> mapEntities)
    {
        return
        [
            new TargetChoice(null, false, ThisEntity),
            new TargetChoice(null, true, "Player"),
            .. mapEntities.OrderBy(x => x.Id).Select(x => new TargetChoice(x.Guid, false, $"{x.Id:000}: {x.Name}", x.Id))
        ];
    }

    // the picked one, this entity when nothing is picked or the entity has since been deleted
    public static TargetChoice Find(TargetChoice[] choices, Movement movement)
    {
        if (movement.TargetsPlayer) return choices[1];
        return choices.FirstOrDefault(x => x.Id is not null && x.Id == movement.EntityId) ?? choices[0];
    }

    // the same for things that aren't movements (a page command's entity)
    public static TargetChoice Find(TargetChoice[] choices, bool player, Guid? id)
    {
        if (player) return choices[1];
        return choices.FirstOrDefault(x => x.Id is not null && x.Id == id) ?? choices[0];
    }
}
