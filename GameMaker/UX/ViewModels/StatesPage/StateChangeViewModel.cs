using GameLibrary.Models;
using GameLibrary.Models.States;
using MercuryLibrary.Extensions;
using MercuryLibrary.WinUI3Components;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.ViewModels.StatesPage;

/// <summary>
/// One row of a state's attribute or element changes. An attribute changes by a flat amount for each stack (0 is no
/// change). An element is shown as the rate the state sets, the way equipment shows it (100% is no change).
/// </summary>
public class StateChangeViewModel(Attribute attribute, State state, bool isElement) : PropertyChangedUpdater
{
    private Dictionary<Guid, int> Changes => isElement ? state.ElementChanges : state.AttributeChanges;

    public string Name => attribute.Name;

    public GameIcon Icon => attribute.Icon;

    // what the box shows when the state doesn't touch this one
    private int NoChange => isElement ? 100 : 0;

    public string Value
    {
        get => Changes.GetValueOrDefault(attribute.Guid, NoChange).ToString();
        set
        {
            if (int.TryParse(value, out var result) && result != NoChange)
            {
                Changes[attribute.Guid] = result;
                OnPropertyChanged();
            }
            else if (value.IsNullOrEmpty() || result == NoChange)
            {
                Changes.Remove(attribute.Guid);
                OnPropertyChanged();
            }
        }
    }
}
