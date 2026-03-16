using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum GameColor
{
    [Display(Name = "#000000")]
    Black,

    [Display(Name = "#ffffff")]
    White,

    [Display(Name = "#e92a0c")]
    Red,

    [Display(Name = "#16c60e")]
    Green,

    [Display(Name = "#1348c6")]
    Blue,

    [Display(Name = "#34c8dc")]
    Cyan,

    [Display(Name = "#c119d0")]
    Magenta,

    [Display(Name = "#e5e93d")]
    Yellow,

    [Display(Name = "#900d0d")]
    Blood,

    [Display(Name = "#010054")]
    Navy,

    [Display(Name = "#844a12")]
    Brown,

    [Display(Name = "#476464")]
    Steel,

    [Display(Name = "#d3d3d3")]
    Silver,

    [Display(Name = "#777777")]
    Gray,

    [Display(Name = "#68048a")]
    Purple
}
