using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ShowTextAction : PageAction
{
    public string? FaceName
    {
        get;
        set => SetField(ref field, value);
    }

    public int FaceIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Title
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Message
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Show Text";
        if (!Title.IsNullOrEmpty()) result += $"\n    Title: {Title}";
        if (!Message.IsNullOrEmpty())
        {
            var message = $"Message: {Message.ReplaceLineEndings(" ").Trim()}";
            if (message.Length > 60) message = message[..60] + "\u2026";
            result += $"\n    \u201c{message}\u201d";
        }

        return result;
    }
}
