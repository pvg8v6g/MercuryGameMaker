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

    public Dictionary<SelfSwitch, bool> SelfSwitchValues { get; init; } = [];

    #endregion
}
