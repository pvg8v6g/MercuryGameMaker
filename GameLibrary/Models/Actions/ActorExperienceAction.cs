using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorExperienceAction : PageAction
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

    public int Experience
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Actor Experience";
        if (!ActorName.IsNullOrEmpty()) result += $" {ActorName}";
        result += $": {Experience:+#;-#;0}";
        return result;
    }
}
