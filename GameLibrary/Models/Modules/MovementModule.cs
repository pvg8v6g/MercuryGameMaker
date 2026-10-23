namespace GameLibrary.Models.Modules;

public partial class MovementModule : Module
{
    public int Speed
    {
        get;
        set => SetField(ref field, value);
    } = 8;

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
