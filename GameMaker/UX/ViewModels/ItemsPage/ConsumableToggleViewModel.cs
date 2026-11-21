using GameLibrary.Models;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.ItemsPage;

public class ConsumableToggleViewModel(string name, GameIcon icon, List<Guid> changeList, Guid guid) : PropertyChangedUpdater
{
    public string Name => name;

    public GameIcon Icon => icon;

    public bool IsChecked
    {
        get => changeList.Contains(guid);
        set
        {
            if (value)
            {
                if (!changeList.Contains(guid))
                    changeList.Add(guid);
            }
            else
            {
                changeList.Remove(guid);
            }
            OnPropertyChanged(nameof(IsChecked));
        }
    }
}
