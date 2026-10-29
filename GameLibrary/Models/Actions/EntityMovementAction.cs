using System.Collections.ObjectModel;
using GameLibrary.Models.Movements;

namespace GameLibrary.Models.Actions;

public partial class EntityMovementAction : PageAction
{
    public Guid? EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public bool WaitForCompletion
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool SkipIfInterrupted
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Repeat
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<Movement> Route { get; init; } = [];

    public override string ToString()
    {
        var result = $"Set Movement Route ({Route.Count} step{(Route.Count == 1 ? "" : "s")})";
        if (Repeat) result += "\n    Repeat";
        if (WaitForCompletion) result += "\n    Wait for completion";
        if (SkipIfInterrupted) result += "\n    Skip if cannot move";
        return result;
    }
}
