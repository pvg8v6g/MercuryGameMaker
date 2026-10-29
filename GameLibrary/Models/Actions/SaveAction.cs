using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class SaveAction : PageAction
{
    public Guid? AnimationId
    {
        get;
        set => SetField(ref field, value);
    }

    public string? AnimationName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool RecoverParty
    {
        get;
        set => SetField(ref field, value);
    }

    public override string ToString() =>
        AnimationName.IsNullOrEmpty() ? "Save Game" : $"Save Game\n    Animation: {AnimationName}";
}
