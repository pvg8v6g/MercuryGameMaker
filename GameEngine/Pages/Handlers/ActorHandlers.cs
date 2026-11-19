using GameEngine.Services.Party;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Fighter;
using GameLibrary.Services.GameData;

namespace GameEngine.Pages.Handlers;

/// <summary>
/// The actor commands change the actor's copy in this game (see the party service), never the game data.
/// An actor command with no actor picked does nothing.
/// </summary>
public abstract class ActorHandler<T>(PartyService partyService) : ActionHandler<T> where T : PageAction
{
    protected PartyService Party => partyService;

    protected override ActionResult Start(PageRun run, T action)
    {
        if (partyService.Actor(ActorId(action)) is { } actor) Change(actor, action);
        return ActionResult.Done;
    }

    protected abstract Guid? ActorId(T action);

    protected abstract void Change(Fighter actor, T action);
}

// Actor Name: the new name, an empty one leaves the name as it is
public class ActorNameHandler(PartyService partyService) : ActorHandler<ActorNameAction>(partyService)
{
    protected override Guid? ActorId(ActorNameAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorNameAction action)
    {
        if (!string.IsNullOrWhiteSpace(action.ActorName)) actor.Name = action.ActorName;
    }
}

public class ActorDescriptionHandler(PartyService partyService) : ActorHandler<ActorDescriptionAction>(partyService)
{
    protected override Guid? ActorId(ActorDescriptionAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorDescriptionAction action) => actor.Description = action.ActorDescription ?? string.Empty;
}

// Actor Image: the face and the character sheet, a part left empty stays as it was. The leader's sprite follows
public class ActorImageHandler(PartyService partyService) : ActorHandler<ActorImageAction>(partyService)
{
    protected override Guid? ActorId(ActorImageAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorImageAction action)
    {
        if (!string.IsNullOrEmpty(action.FaceName))
        {
            actor.FaceName = action.FaceName;
            actor.FaceIndex = action.FaceIndex;
        }

        if (!string.IsNullOrEmpty(action.CharacterName))
        {
            actor.CharacterName = action.CharacterName;
            actor.CharacterIndex = action.CharacterIndex;
        }

        Party.ImageChanged(actor.Guid);
    }
}

// Actor Attribute: adds the amount to the attribute (a negative amount lowers it), never below 0
public class ActorAttributeHandler(PartyService partyService) : ActorHandler<ActorAttributeAction>(partyService)
{
    protected override Guid? ActorId(ActorAttributeAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorAttributeAction action)
    {
        if (action.AttributeId is not { } attribute) return;
        actor.Attributes[attribute] = Math.Max(0, actor.Attributes.GetValueOrDefault(attribute) + action.Amount);
    }
}

// Actor Life: adds the amount (negative hurts), between 0 and the actor's max life
public class ActorLifeHandler(PartyService partyService) : ActorHandler<ActorLifeAction>(partyService)
{
    protected override Guid? ActorId(ActorLifeAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorLifeAction action) => actor.Life = Math.Clamp(actor.Life + action.LifeValue, 0, actor.MaxLife);
}

public class ActorManaHandler(PartyService partyService) : ActorHandler<ActorManaAction>(partyService)
{
    protected override Guid? ActorId(ActorManaAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorManaAction action) => actor.Mana = Math.Clamp(actor.Mana + action.ManaValue, 0, actor.MaxMana);
}

// Actor Experience: adds the amount, never below 0. Levels don't come from experience yet, there is no experience table
public class ActorExperienceHandler(PartyService partyService) : ActorHandler<ActorExperienceAction>(partyService)
{
    protected override Guid? ActorId(ActorExperienceAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorExperienceAction action) => actor.Experience = Math.Max(0, actor.Experience + action.Experience);
}

// Actor Level: adds the amount of levels (negative takes them away), the discipline's growths set the stats
public class ActorLevelHandler(PartyService partyService) : ActorHandler<ActorLevelAction>(partyService)
{
    protected override Guid? ActorId(ActorLevelAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorLevelAction action) => Party.SetLevel(actor, actor.Level + action.Level);
}

// Actor Arte: learns or forgets the arte
public class ActorArteHandler(PartyService partyService) : ActorHandler<ActorArteAction>(partyService)
{
    protected override Guid? ActorId(ActorArteAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorArteAction action)
    {
        if (action.ArteId is not { } arte) return;

        if (!action.Add) actor.Artes.Remove(arte);
        else if (!actor.Artes.Contains(arte)) actor.Artes.Add(arte);
    }
}

public class ActorStateHandler(PartyService partyService) : ActorHandler<ActorStateAction>(partyService)
{
    protected override Guid? ActorId(ActorStateAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorStateAction action)
    {
        if (action.StateId is not { } state) return;

        if (!action.Add) actor.States.Remove(state);
        else if (!actor.States.Contains(state)) actor.States.Add(state);
    }
}

/// <summary>
/// Actor Equipment: equips the piece in its slot (an accessory takes the first free accessory slot) or takes it off.
/// What comes off goes into the party's items, a piece the party carries is taken out of them. Locked equipment stays.
/// </summary>
public class ActorEquipmentHandler(PartyService partyService, IGameDataService gameDataService) : ActorHandler<ActorEquipmentAction>(partyService)
{
    protected override Guid? ActorId(ActorEquipmentAction action) => action.ActorId;

    protected override void Change(Fighter actor, ActorEquipmentAction action)
    {
        if (actor.LockEquipment || action.EquipmentId is not { } id) return;

        if (!action.Equip)
        {
            foreach (var worn in actor.Equipment.Values.Where(x => x.Id == id))
            {
                worn.Id = null;
                Party.ChangeItem(id, 1);
            }

            return;
        }

        if (gameDataService.Equipment.FirstOrDefault(x => x.Guid == id) is not { } equipment) return;

        var slots = actor.Equipment.Values.Where(x => x.Location == equipment.EquipmentLocation).ToArray();
        if ((slots.FirstOrDefault(x => x.Id is null) ?? slots.FirstOrDefault()) is not { } slot) return;

        if (slot.Id is { } old) Party.ChangeItem(old, 1);
        slot.Id = id;
        Party.ChangeItem(id, -1);
    }
}
