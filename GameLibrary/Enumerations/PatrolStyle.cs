using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

// how a patroller goes from point to point
public enum PatrolStyle
{
    // 1, 2, 3, 1, 2, 3...
    [Display(Name = "Loop")]
    Loop,

    // 1, 2, 3, 2, 1, 2...
    [Display(Name = "Back and Forth")]
    BackAndForth,

    // any point after any other, picked at random
    [Display(Name = "Wander")]
    Wander
}
