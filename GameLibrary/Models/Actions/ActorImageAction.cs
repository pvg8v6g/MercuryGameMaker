using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ActorImageAction : PageAction
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

    public string? CharacterName
    {
        get;
        set => SetField(ref field, value);
    }

    public int CharacterIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString()
    {
        var result = "Actor Image";
        if (!ActorName.IsNullOrEmpty()) result += $" {ActorName}";
        if (!CharacterName.IsNullOrEmpty()) result += $"\n    Character: {CharacterName} [{CharacterIndex}]";
        if (!FaceName.IsNullOrEmpty()) result += $"\n    Face: {FaceName} [{FaceIndex}]";
        return result;
    }
}
