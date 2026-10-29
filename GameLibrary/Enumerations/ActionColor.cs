using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum ActionColor
{
    [Display(Name = "#f26d6d")]
    Message,

    [Display(Name = "#6da8f2")]
    Flow,

    [Display(Name = "#6dd47e")]
    Party,

    [Display(Name = "#e6c84d")]
    Actor,

    [Display(Name = "#c08de6")]
    Entity,

    [Display(Name = "#4dc8c8")]
    Media,

    [Display(Name = "#f28dc0")]
    System
}
