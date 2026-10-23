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

    public override string ToString() => "Show Text";
}
