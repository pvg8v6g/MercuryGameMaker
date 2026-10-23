using GameLibrary.Enumerations;

namespace GameLibrary.Models.Modules;

public partial class PositionModule : Module
{
    public double X
    {
        get;
        set => SetField(ref field, value);
    }

    public double Y
    {
        get;
        set => SetField(ref field, value);
    }

    public double Z
    {
        get;
        set => SetField(ref field, value);
    }

    public double SortZ
    {
        get;
        set => SetField(ref field, value);
    }

    public Direction Direction
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;
}
