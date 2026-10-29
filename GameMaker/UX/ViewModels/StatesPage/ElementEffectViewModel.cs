using GameLibrary.Models.States;
using MercuryLibrary.WinUI3Components;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.ViewModels.StatesPage;

public class ElementEffectViewModel(Attribute element, State state) : PropertyChangedUpdater
{
    public string Name => element.Name;

    public int Icon => element.Icon;

    public bool IsAffected
    {
        get => state.ElementEffect.Contains(element.Guid);
        set
        {
            if (value)
            {
                if (!state.ElementEffect.Contains(element.Guid))
                    state.ElementEffect.Add(element.Guid);
            }
            else
            {
                state.ElementEffect.Remove(element.Guid);
            }
            OnPropertyChanged(nameof(IsAffected));
        }
    }
}
