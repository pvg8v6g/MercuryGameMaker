using System.Collections.ObjectModel;

namespace GameLibrary.Models;

public partial class Entity : BaseModel
{
    #region Properties

    public Guid MapId
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<Page> Pages { get; init; } = [];

    #endregion
}
