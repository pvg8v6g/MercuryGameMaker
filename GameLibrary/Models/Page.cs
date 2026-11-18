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

    // the entity turns to the player when the player starts the page (action button or touch), off for things like chests
    public bool FacePlayer
    {
        get;
        set => SetField(ref field, value);
    } = true;

    #endregion
}
