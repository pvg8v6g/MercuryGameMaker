using System.Collections.ObjectModel;
using GameLibrary.Enumerations;

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

    // arte projectiles only: who the projectile detonates on when it touches them. A contact projectile that reaches
    // its Detonate step without touching anyone is only removed, its arte doesn't go off
    public ContactTarget DetonateOnContact
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
