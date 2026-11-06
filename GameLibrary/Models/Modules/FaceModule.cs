namespace GameLibrary.Models.Modules;

// the face shown next to this page's messages, from Graphics\Faces
public partial class FaceModule : Module
{
    public string? FaceName
    {
        get;
        set => SetField(ref field, value);
    }

    // the cell on the face sheet, counted left to right then top to bottom
    public int? FaceIndex
    {
        get;
        set => SetField(ref field, value);
    }
}
