using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum MapAreaType
{
    Area,
    Collision,
    Water,
    Footstep,

    // moves whoever steps into it to the area's level, the top and bottom of stairs or a ramp
    [Display(Name = "Level Change")]
    LevelChange,

    // shimmering air over the ground, lava or a hot vent. It bends the ground under it, never the props or characters
    [Display(Name = "Heat Haze")]
    HeatHaze
}
