using GameLibrary.Models.Fighter;
using MercuryLibrary.Extensions;
using MercuryLibrary.WinUI3Components;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.ViewModels.EnemiesPage;

public class EnemyAttributeViewModel(Attribute attribute, Fighter fighter) : PropertyChangedUpdater
{
    #region Properties

    public string Name => attribute.Name;

    public int Icon => attribute.Icon;

    public string AttributeValue
    {
        get => fighter.Attributes.GetValueOrDefault(attribute.Guid, 0).ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                if (result == 0) fighter.Attributes.Remove(attribute.Guid);
                else fighter.Attributes[attribute.Guid] = result;
                OnPropertyChanged();
            }
            else if (value.IsNullOrEmpty())
            {
                fighter.Attributes.Remove(attribute.Guid);
                OnPropertyChanged();
            }
        }
    }

    #endregion
}
