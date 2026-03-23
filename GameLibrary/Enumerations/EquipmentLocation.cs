using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum EquipmentLocation
{
    [Display(Name = "Weapon")]
    Weapon,

    [Display(Name = "Head")]
    Head,

    [Display(Name = "Chest")]
    Chest,

    [Display(Name = "Hands")]
    Hands,

    [Display(Name = "Waist")]
    Waist,

    [Display(Name = "Legs")]
    Legs,

    [Display(Name = "Feet")]
    Feet,

    [Display(Name = "Accessory")]
    Accessory,

    [Display(Name = "Artifact")]
    Artifact
}
