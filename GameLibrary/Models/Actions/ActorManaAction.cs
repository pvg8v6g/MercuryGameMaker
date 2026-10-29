using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorManaAction : PageAction
{
    public Guid? ActorId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ActorName
    {
        get;
        set => SetField(ref field, value);
    }

    public int ManaValue
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Actor Mana";
        if (!ActorName.IsNullOrEmpty()) result += $" {ActorName}";
        result += $": {ManaValue:+#;-#;0}";
        return result;
    }
}
