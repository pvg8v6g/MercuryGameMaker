using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.ArtesPage;

public class ArteToggleViewModel(string name, int icon, List<Guid> changeList, Guid guid) : PropertyChangedUpdater
{
    public string Name => name;

    public int Icon => icon;

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
