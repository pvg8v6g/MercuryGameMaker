namespace GameLibrary.Models.Modules;

public partial class MovementModule : Module
{
    // the normal walking speed (the player's on the story maps), what every new walker and Set Speed step starts at
    public const int DefaultSpeed = 8;

    public int Speed
    {
        get;
        set => SetField(ref field, value);
    } = DefaultSpeed;

    public bool Dashing
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool Stepping
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool AutoStepping
    {
        get;
        set => SetField(ref field, value);
    }

    public bool FixDirection
    {
        get;
        set => SetField(ref field, value);
    }
}
