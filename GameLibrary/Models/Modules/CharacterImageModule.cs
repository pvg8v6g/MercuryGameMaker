namespace GameLibrary.Models.Modules;

public partial class CharacterImageModule : Module
{
    public string? ImageName
    {
        get;
        set => SetField(ref field, value);
    }

    public int Index
    {
        get;
        set => SetField(ref field, value);
    }

    public int SteppingIndex
    {
        get;
        set => SetField(ref field, value);
    }
}
