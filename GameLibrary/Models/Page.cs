using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models.Actions;
using GameLibrary.Models.Conditions;
using GameLibrary.Models.Modules;

namespace GameLibrary.Models;

public partial class Page : BaseModel
{
    #region Properties

    public Guid EntityId
    {
        get;
        set => SetField(ref field, value);
    }

    public PageTrigger PageTrigger
    {
        get;
        set => SetField(ref field, value);
    } = PageTrigger.Action;

    public ObservableCollection<PageAction> Actions { get; init; } = [];

    public ObservableCollection<Module> Modules { get; init; } = [];

    public ObservableCollection<Condition> Conditions { get; init; } = [];

    // leaving the map and coming back finds the entity where it was (and facing the same way), off: back where it was placed
    public bool RemembersPosition
    {
        get;
        set => SetField(ref field, value);
    } = true;

    #endregion
}
