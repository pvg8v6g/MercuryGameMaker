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

    public Dictionary<Direction, ObservableCollection<Area>> Hitboxes
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

    // No idea what this is right now
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

    #endregion

    #region Equipment

    [JsonInclude]
    [JsonPropertyName("Equipment")]
    private Dictionary<string, EquippedSlot> _equipment = new()
    {
        ["Weapon"] = new EquippedSlot { Location = EquipmentLocation.Weapon, Id = null },
        ["Head"] = new EquippedSlot { Location = EquipmentLocation.Head, Id = null },
        ["Chest"] = new EquippedSlot { Location = EquipmentLocation.Chest, Id = null },
        ["Hands"] = new EquippedSlot { Location = EquipmentLocation.Hands, Id = null },
        ["Waist"] = new EquippedSlot { Location = EquipmentLocation.Waist, Id = null },
        ["Legs"] = new EquippedSlot { Location = EquipmentLocation.Legs, Id = null },
        ["Feet"] = new EquippedSlot { Location = EquipmentLocation.Feet, Id = null },
        ["Accessory 1"] = new EquippedSlot { Location = EquipmentLocation.Accessory, Id = null },
        ["Accessory 2"] = new EquippedSlot { Location = EquipmentLocation.Accessory, Id = null },
        ["Artifact"] = new EquippedSlot { Location = EquipmentLocation.Artifact, Id = null }
    };

    [JsonIgnore]
    public IReadOnlyDictionary<string, EquippedSlot> Equipment => _equipment;

    #endregion

    #region Enemy Properties

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
    } = [];

    #endregion

    #region In Game Properties

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

    public ObservableCollection<Guid> Artes
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public ObservableCollection<Guid> States
    {
        get;
        set => SetField(ref field, value);
    } = [];

    #endregion"
}
