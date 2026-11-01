namespace GameMaker.UX.Models.MainPage;

// a combo box entry pointing at a map (or nothing when Guid is null)
public sealed record MapOption(Guid? Guid, string Name)
{
    public override string ToString() => Name;
}
