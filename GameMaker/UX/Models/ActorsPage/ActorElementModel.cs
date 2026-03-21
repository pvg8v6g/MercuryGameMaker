using GameLibrary.Models.Fighter;
using MercuryLibrary.WinUI3Components;
using MercuryLibrary.Extensions;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.Models.ActorsPage;

public class ActorElementModel(Attribute attribute, Fighter fighter) : PropertyChangedUpdater
{
    #region Properties

    public string Name => attribute.Name;

    public int Icon => attribute.Icon;

    public string ElementValue
    {
        get => fighter.Elements.GetValueOrDefault(attribute.Guid, 100).ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                fighter.Elements[attribute.Guid] = result;
                OnPropertyChanged();
            }
            else if (value.IsNullOrEmpty())
            {
                fighter.Elements.Remove(attribute.Guid);
                OnPropertyChanged();
            }
        }
    }

    #endregion
}
