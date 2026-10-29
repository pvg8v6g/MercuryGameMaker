using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorLifeAction : PageAction
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

    public int LifeValue
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Actor Life";
        if (!ActorName.IsNullOrEmpty()) result += $" {ActorName}";
        result += $": {LifeValue:+#;-#;0}";
        return result;
    }
}
