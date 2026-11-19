using GameLibrary.Models;

namespace GameMaker.UX.Models.Choices;

/// <summary>
/// One option of a dropdown that picks a saved thing by its guid, with a first option that means none of them
/// (no balloon, the route's own entity). A plain list instead of a typeable box, so a pick can always go back.
/// </summary>
public record NamedChoice(Guid? Id, string Name)
{
    public override string ToString() => Name;

    // the first option, then one per model by name
    public static NamedChoice[] List(string noneText, IEnumerable<BaseModel> models)
    {
        return [new NamedChoice(null, noneText), .. models.Select(x => new NamedChoice(x.Guid, x.Name))];
    }

    // the option for a guid, the first one when nothing (or something since deleted) is picked
    public static NamedChoice Find(NamedChoice[] choices, Guid? id)
    {
        return choices.FirstOrDefault(x => x.Id == id) ?? choices[0];
    }
}
