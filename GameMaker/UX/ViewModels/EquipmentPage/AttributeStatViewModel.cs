using GameLibrary.Models.Items;
using MercuryLibrary.Extensions;
using MercuryLibrary.WinUI3Components;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.ViewModels.EquipmentPage;

public class AttributeStatViewModel(Attribute attribute, Equipment equipment) : PropertyChangedUpdater
{
    public string Name => attribute.Name;

    public int Icon => attribute.Icon;

    public string AttributeValue
    {
        get => equipment.AttributeStats.GetValueOrDefault(attribute.Guid, 0).ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                equipment.AttributeStats[attribute.Guid] = result;
                OnPropertyChanged();
            }
            else if (value.IsNullOrEmpty())
            {
                equipment.AttributeStats.Remove(attribute.Guid);
                OnPropertyChanged();
            }
        }
    }

    public string ElementValue
    {
        get => equipment.ElementResist.GetValueOrDefault(attribute.Guid, 100).ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                equipment.ElementResist[attribute.Guid] = result;
                OnPropertyChanged();
            }
            else if (value.IsNullOrEmpty())
            {
                equipment.ElementResist.Remove(attribute.Guid);
                OnPropertyChanged();
            }
        }
    }
}
