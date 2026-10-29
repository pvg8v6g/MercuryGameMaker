using System.Collections.ObjectModel;
using MercuryLibrary.Extensions;

namespace GameLibrary.Models.Actions;

public partial class ShowChoicesAction : PageAction
{
    /// <summary>Variable the chosen index is stored in.</summary>
    public string? Variable
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<Choice> Choices { get; init; } = [];

    public override string ToString()
    {
        var result = "Show Choices";
        if (!Variable.IsNullOrEmpty()) result += $"\n    \u2192 {Variable}";
        foreach (var choice in Choices)
        {
            result += $"\n    \u2022 {choice.Text}";
        }

        return result;
    }
}
