using System.ComponentModel.DataAnnotations;

namespace GameLibrary.Enumerations;

public enum BranchConditionType
{
    [Display(Name = "Switch")]
    Switch = 0,

    [Display(Name = "Variable")]
    Variable = 1,

    [Display(Name = "Self Switch")]
    SelfSwitch = 2,

    [Display(Name = "Timer")]
    Timer = 3,

    [Display(Name = "Actor In Party")]
    ActorInParty = 4,

    [Display(Name = "Actor Has Name")]
    ActorHasName = 5,

    [Display(Name = "Actor Is Discipline")]
    ActorIsDiscipline = 6,

    [Display(Name = "Actor Has Arte")]
    ActorHasArte = 7,

    [Display(Name = "Actor Equipment")]
    ActorEquipment = 8,

    [Display(Name = "Actor Has State")]
    ActorHasState = 9,

    [Display(Name = "Enemy Life")]
    EnemyLife = 10,

    [Display(Name = "Enemy Has State")]
    EnemyHasState = 11,

    [Display(Name = "Entity Direction")]
    EntityDirection = 12,

    [Display(Name = "Party Has Item")]
    PartyHasItem = 13,

    [Display(Name = "Party Has Money")]
    PartyHasMoney = 14
}
