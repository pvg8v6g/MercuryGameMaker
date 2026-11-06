using System.Collections.ObjectModel;
using GameLibrary.Models.GameMaps;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Models.MainPage;

public partial class MapTreeNode(GameMap map) : PropertyChangedUpdater
{
    #region Properties

    public GameMap Map => map;

    public Symbol Symbol => map.FolderOnly ? Symbol.Folder : Symbol.Map;

    // padded like the map pickers (001), the tree is rebuilt whenever an id changes so this never goes stale
    public string IdText => $"{map.Id:000}";

    public ObservableCollection<MapTreeNode> Children { get; } = [];

    public bool IsExpanded
    {
        get => map.IsExpanded;
        set
        {
            if (map.IsExpanded == value) return;
            map.IsExpanded = value;
            OnPropertyChanged();
        }
    }

    #endregion
}
