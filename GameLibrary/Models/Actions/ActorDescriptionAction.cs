using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorDescriptionAction : PageAction
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

    public string? ActorDescription
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Actor Description";
        if (!ActorName.IsNullOrEmpty()) result += $" {ActorName}";

        if (!ActorDescription.IsNullOrEmpty())
        {
            var description = ActorDescription.ReplaceLineEndings(" ").Trim();
            if (description.Length > 60) description = description[..60] + "\u2026";
            result += $"\n    \u201c{description}\u201d";
        }

        return result;
    }
}
