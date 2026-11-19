namespace GameLibrary.Models.Actions;

/// <summary>
/// An action done to an entity on the map: the page's own (no entity and not the player), the player, or another
/// entity by its guid. TargetName is the picked one's "id: name", kept for the action list.
/// </summary>
public interface IEntityTargetAction
{
    Guid? EntityId { get; set; }

    bool TargetsPlayer { get; set; }

    string? TargetName { get; set; }
}

public static class EntityTargetActions
{
    // who the action is done to, for the action list
    public static string TargetText(this IEntityTargetAction action)
    {
        if (action.TargetsPlayer) return "Player";
        return action.EntityId is null || string.IsNullOrEmpty(action.TargetName) ? "This Entity" : action.TargetName;
    }
}
