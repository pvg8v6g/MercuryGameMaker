using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorLevelAction : PageAction
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

    public int Level
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Actor Level";
        if (!ActorName.IsNullOrEmpty()) result += $" {ActorName}";
        result += $": {Level:+#;-#;0}";
        return result;
    }
}
