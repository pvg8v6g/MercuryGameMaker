using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum MovementTarget
{
    [Display(Name = "Location")]
    Location = 0,

    [Display(Name = "Player")]
    Player = 1,

    [Display(Name = "Entity")]
    Entity = 2
}
