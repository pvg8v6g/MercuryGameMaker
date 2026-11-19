using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Movements;

/// <summary>
/// Balloon Icon: plays a balloon from the Balloon Icons settings over the entity, once through its row.
/// </summary>
public partial class BalloonMovement : Movement
{
    // the balloon in the Balloon Icons settings, none shows nothing
    public Guid? BalloonIconId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? BalloonIconName
    {
        get;
        set => SetField(ref field, value);
    }

    // the route waits until the balloon has gone before its next step
    public bool WaitForCompletion
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = BalloonIconName.IsNullOrEmpty() ? "Balloon Icon" : $"Balloon Icon: {BalloonIconName}";
        return WaitForCompletion ? result + " (wait)" : result;
    }
}
