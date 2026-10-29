using System.Collections.ObjectModel;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Media;

public partial class MediaSelectionModel : PropertyChangedUpdater
{
    #region Properties

    public string? Name
    {
        get;
        set => SetField(ref field, value);
    }

    public string? Path
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<MediaSelectionModel> Children { get; } = [];

    #endregion
}
