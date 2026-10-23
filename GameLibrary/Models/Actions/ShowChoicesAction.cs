using System.Collections.ObjectModel;

namespace GameLibrary.Models.Actions;

public partial class ShowChoicesAction : PageAction
{
    /// <summary>Variable the chosen index is stored in.</summary>
    public string? Variable
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<string> Choices { get; init; } = [];

    public override string ToString() => "Show Choices";
}
