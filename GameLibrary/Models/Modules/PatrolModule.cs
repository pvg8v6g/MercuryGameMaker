namespace GameLibrary.Models.Modules;

/// <summary>
/// Patrol and chase: the entity walks a patrol path (or wanders a zone), and when it sees the player it chases them.
/// Walking into the player runs the page's event touch. It gives up when it loses sight of the player for a while,
/// can't get any closer, or the chase runs too long, and walks back to the nearest point of its patrol.
/// </summary>
public partial class PatrolModule : Module
{
    // the patrol path on the map, by name. None: it wanders the zone, or stands guard where it was placed
    public string? PathName
    {
        get;
        set => SetField(ref field, value);
    }

    // an area on the map, by name, to wander in when there's no path
    public string? ZoneName
    {
        get;
        set => SetField(ref field, value);
    }

    // the path point to start from, 1 is the first
    public int StartPoint
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    // walking speed while patrolling, the player's is 8
    public int PatrolSpeed
    {
        get;
        set => SetField(ref field, value);
    } = 4;

    // off: it only patrols and never chases
    public bool Chases
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public int ChaseSpeed
    {
        get;
        set => SetField(ref field, value);
    } = 8;

    // how far it sees, in tiles
    public int SightRange
    {
        get;
        set => SetField(ref field, value);
    } = 5;

    // off: it only sees what's in front of it
    public bool SeesAllAround
    {
        get;
        set => SetField(ref field, value);
    }

    // milliseconds without seeing the player before it gives up
    public int LoseSightTime
    {
        get;
        set => SetField(ref field, value);
    } = 3000;

    // milliseconds a chase can last before it gives up, 0 is no limit
    public int ChaseTimeLimit
    {
        get;
        set => SetField(ref field, value);
    } = 15000;

    // the balloon from the Balloon Icons settings shown when it spots the player, none shows nothing
    public Guid? SpotBalloonId
    {
        get;
        set => SetField(ref field, value);
    }

    // milliseconds it stands still after spotting the player, before the chase
    public int SpotPause
    {
        get;
        set => SetField(ref field, value);
    } = 500;
}
