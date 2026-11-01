using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Modules;

namespace GameLibrary.Models;

/// <summary>
/// The entity the game view follows the player with. It lives on every map but is never saved with one.
/// </summary>
public partial class Camera : Entity
{
    #region Constructor

    public Camera()
    {
        Id = -1;
        Name = "Camera";

        var positionModule = new PositionModule { X = 0, Y = 0, Z = 5000 };

        var hitboxModule = new EntityHitboxModule { ShowHitboxes = false, Collide = false };
        hitboxModule.Hitboxes[Direction.None] = [new Hitbox { OffsetX = 0, OffsetY = 0, Width = 48, Height = 48, Color = GameColor.Cyan }];

        var page = new Page { EntityId = Guid, Id = 1, Name = "Camera" };
        page.Modules.Add(positionModule);
        page.Modules.Add(hitboxModule);
        Pages.Add(page);
    }

    #endregion
}
