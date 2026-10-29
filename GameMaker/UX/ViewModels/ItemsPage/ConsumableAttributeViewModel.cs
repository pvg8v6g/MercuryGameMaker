using GameLibrary.Models.Items;
using MercuryLibrary.Extensions;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.ItemsPage;

public class ConsumableAttributeViewModel(string name, int icon, Guid guid, Consumable consumable) : PropertyChangedUpdater
{
    public string Name => name;

    public int Icon => icon;

    public string AttributeValue
    {
        get => consumable.AttributeChanges.GetValueOrDefault(guid, 0).ToString();
        set
        {
            if (int.TryParse(value, out var result))
            {
                consumable.AttributeChanges[guid] = result;
                OnPropertyChanged();
            }
            else if (value.IsNullOrEmpty())
            {
                consumable.AttributeChanges.Remove(guid);
                OnPropertyChanged();
            }
        }
    }
}
