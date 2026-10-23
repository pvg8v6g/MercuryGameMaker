using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;

namespace GameLibrary.Models.Modules;

public partial class EntityHitboxModule : Module
{
    public bool ShowHitboxes
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Collide
    {
        get;
        set => SetField(ref field, value);
    }

    public Dictionary<Direction, ObservableCollection<Area>> Hitboxes { get; init; } = [];
}
