using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum FlowConditionType
{
    [Display(Name = "Switch")]
    Switch = 0,

    [Display(Name = "Variable")]
    Variable = 1,

    [Display(Name = "Self Switch")]
    SelfSwitch = 2,

    [Display(Name = "Timer")]
    Timer = 3
}
