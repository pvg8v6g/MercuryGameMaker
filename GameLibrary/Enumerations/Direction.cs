using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum Direction
{
    [Display(Name = "- None -")]
    None = -1,

    [Display(Name = "Down")]
    Down = 0,

    [Display(Name = "Left")]
    Left = 1,

    [Display(Name = "Right")]
    Right = 2,

    [Display(Name = "Up")]
    Up = 3
}
