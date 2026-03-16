using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;

namespace GameLibrary.Models.Fighter;

public partial class Fighter : BaseModel
{
    #region Properties

    public string Description
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public Guid? DisciplineGuid
    {
        get;
        set => SetField(ref field, value);
    }

    public string? CharacterName
    {
        get;
        set => SetField(ref field, value);
    }

    public int CharacterIndex
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public Direction CharacterDirection
    {
        get;
        set => SetField(ref field, value);
    } = Direction.Down;

    public string? FaceName
    {
        get;
        set => SetField(ref field, value);
    }

    public int FaceIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public Dictionary<Direction, ObservableCollection<Hitbox>> Hitboxes
    {
        get;
        set => SetField(ref field, value);
    } = new();

    public int Life
    {
        get;
        set => SetField(ref field, value);
    }

    public int MaxLife
    {
        get;
        set => SetField(ref field, value);
    } = 100;

    public int Mana
    {
        get;
        set => SetField(ref field, value);
    }

    public int MaxMana
    {
        get;
        set => SetField(ref field, value);
    } = 50;

    public int Shield
    {
        get;
        set => SetField(ref field, value);
    }

    public int MaxShield
    {
        get;
        set => SetField(ref field, value);
    }

    public bool StaticStats
    {
        get;
        set => SetField(ref field, value);
    }

    public Dictionary<Guid, int> Attributes
    {
        get;
        set => SetField(ref field, value);
    } = new();

    public Dictionary<Guid, int> Elements
    {
        get;
        set => SetField(ref field, value);
    } = new();

    public Guid Weapon
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Head
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Chest
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Gloves
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Sash
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Legs
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Feet
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Accessory1
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Accessory2
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid Artifact
    {
        get;
        set => SetField(ref field, value);
    }

    public bool LockEquipment
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Medic
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid AutoAnimation
    {
        get;
        set => SetField(ref field, value);
    }

    public int Level
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public int Experience
    {
        get;
        set => SetField(ref field, value);
    }

    public int MonetaryValue
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<DropModel> Drops
    {
        get;
        set => SetField(ref field, value);
    } = new();

    [JsonIgnore]
    public int Team
    {
        get;
        set => SetField(ref field, value);
    }

    public int OriginalTeam
    {
        get;
        set => SetField(ref field, value);
    }

    [JsonIgnore]
    public int Threat
    {
        get;
        set => SetField(ref field, value);
    }

    public bool CanUseItems
    {
        get;
        set => SetField(ref field, value);
    }

    public bool FleeDanger
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Boss
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Undead
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<Guid> Artes
    {
        get;
        set => SetField(ref field, value);
    } = new();

    public ObservableCollection<Guid> States
    {
        get;
        set => SetField(ref field, value);
    } = new();

    #endregion
}
