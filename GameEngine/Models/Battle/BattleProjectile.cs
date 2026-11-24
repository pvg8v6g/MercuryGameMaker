using Windows.Foundation;
using GameEngine.Battle;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Artes;

namespace GameEngine.Models.Battle;

/// <summary>
/// A projectile flying in the battle: its body on the map, the arte it belongs to, and the attacker snapshot taken when
/// it was fired, so whatever it lands is worked out with the numbers of the fighter who fired it at that moment.
/// It walks its page's movement routes one after another, and the battle runner removes it when it detonates.
/// </summary>
public class BattleProjectile(MapEntity body, Arte arte, AttackerSnapshot attacker, IReadOnlyList<EntityMovementAction> routes, ContactTarget contact, bool detonatesAtEnd, int depth)
{
    #region Properties

    public MapEntity Body => body;

    // the arte that goes off when it detonates
    public Arte Arte => arte;

    public AttackerSnapshot Attacker => attacker;

    // the page's own movement routes, walked in order
    public IReadOnlyList<EntityMovementAction> Routes => routes;

    // the route being walked, past the end once they've all been walked
    public int RouteIndex { get; set; } = -1;

    public ContactTarget Contact => contact;

    // a contact projectile that touched nobody still goes off with the full arte at its Detonate step
    public bool DetonatesAtEnd => detonatesAtEnd;

    // how many artes deep it was fired: 0 from a cast, 1 from an arte a projectile activated, and so on
    public int Depth => depth;

    // updates it has been on the map. Its hitboxes are only placed by the systems from its second update on
    public int Updates { get; set; }

    // where its hitboxes were last update, so a fast projectile can't skip over a fighter between two updates
    public Rect? LastBounds { get; set; }

    public bool IsGone { get; set; }

    #endregion
}
