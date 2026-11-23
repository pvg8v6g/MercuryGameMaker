using GameLibrary.Models.Actions;
using GameLibrary.Models.Movements;

namespace GameLibrary.Models.Artes;

/// <summary>
/// A chain is an arte whose projectile activates another arte that fires projectiles of its own, which can activate
/// another, and so on. The battle stops a chain after <see cref="MaxDepth" /> steps so a loop can't run away, and the
/// editor uses the same check to warn that an arte's chain will be cut off.
/// </summary>
public static class ArteChains
{
    public const int MaxDepth = 4;

    /// <summary>
    /// Whether the battle would cut this arte's chain short: a projectile that has already been through
    /// <see cref="MaxDepth" /> activations activates another arte that fires projectiles. Looks at the same routes the
    /// battle walks: the top-level routes of the page the projectile runs on.
    /// </summary>
    public static bool IsCutOff(Arte arte, Func<Guid, Arte?> findArte) => IsCutOff(arte, findArte, 0);

    private static bool IsCutOff(Arte arte, Func<Guid, Arte?> findArte, int depth)
    {
        foreach (var projectile in arte.Projectiles)
        {
            var page = projectile.Pages.OrderByDescending(x => x.Id).FirstOrDefault(x => x.Conditions.Count == 0)
                       ?? projectile.Pages.OrderByDescending(x => x.Id).FirstOrDefault();
            if (page is null) continue;

            var activated = page.Actions.OfType<EntityMovementAction>()
                .Where(x => x.EntityId is null)
                .SelectMany(x => x.Route)
                .OfType<ActivateArteMovement>()
                .Select(x => x.ArteId)
                .OfType<Guid>();
            foreach (var id in activated)
            {
                if (findArte(id) is not { Projectiles.Count: > 0 } next) continue;
                if (depth >= MaxDepth || IsCutOff(next, findArte, depth + 1)) return true;
            }
        }

        return false;
    }
}
