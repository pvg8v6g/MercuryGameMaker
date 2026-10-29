using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using GameLibrary.Utilities;

namespace GameLibrary.Models.Actions;

public partial class BranchAction : PageAction
{
    /// <summary>Determines which condition is evaluated. The condition options are mutually exclusive.</summary>
    public BranchConditionType ConditionType
    {
        get;
        set => SetField(ref field, value);
    }

    #region Switch / Variable / Self Switch / Timer

    public string? SwitchConditionName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool SwitchConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public string? VariableConditionName
    {
        get;
        set => SetField(ref field, value);
    }

    public string? VariableConditionValue
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? SelfSwitchConditionEntity
    {
        get;
        set => SetField(ref field, value);
    }

    public SelfSwitch? SelfSwitchConditionKey
    {
        get;
        set => SetField(ref field, value);
    }

    public bool SelfSwitchConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public string? TimerConditionName
    {
        get;
        set => SetField(ref field, value);
    }

    public Comparison? TimerConditionComparison
    {
        get;
        set => SetField(ref field, value);
    }

    public int? TimerConditionValue
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Actor / Enemy

    public Guid? ActorInPartyConditionActor
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ActorInPartyConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Guid? ActorHasNameConditionActor
    {
        get;
        set => SetField(ref field, value);
    }

    public string? ActorHasNameConditionName
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ActorHasNameConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Guid? ActorIsDisciplineConditionActor
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? ActorIsDisciplineConditionDiscipline
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ActorIsDisciplineConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Guid? ActorHasArteConditionActor
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? ActorHasArteConditionArte
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ActorHasArteConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Guid? ActorEquipmentConditionActor
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? ActorEquipmentConditionEquipment
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ActorEquipmentConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Guid? ActorHasStateConditionActor
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? ActorHasStateConditionState
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ActorHasStateConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Guid? EnemyLifeConditionEnemy
    {
        get;
        set => SetField(ref field, value);
    }

    public Comparison? EnemyLifeConditionComparison
    {
        get;
        set => SetField(ref field, value);
    }

    public int? EnemyLifeConditionValue
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? EnemyHasStateConditionEnemy
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? EnemyHasStateConditionState
    {
        get;
        set => SetField(ref field, value);
    }

    public bool EnemyHasStateConditionValue
    {
        get;
        set => SetField(ref field, value);
    } = true;

    #endregion

    #region Entity / Party

    public Guid? EntityDirectionConditionEntity
    {
        get;
        set => SetField(ref field, value);
    }

    public Direction? EntityDirectionConditionDirection
    {
        get;
        set => SetField(ref field, value);
    }

    public Guid? PartyHasItemConditionItem
    {
        get;
        set => SetField(ref field, value);
    }

    public Comparison? PartyHasItemConditionComparison
    {
        get;
        set => SetField(ref field, value);
    }

    public int? PartyHasItemConditionValue
    {
        get;
        set => SetField(ref field, value);
    }

    public Comparison? PartyHasMoneyConditionComparison
    {
        get;
        set => SetField(ref field, value);
    }

    public int? PartyHasMoneyConditionValue
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Branches

    public ObservableCollection<PageAction> BranchActions { get; init; } = [];

    public ObservableCollection<PageAction> ElseActions { get; init; } = [];

    public bool ElseBranch
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    public override string ToString() => $"Branch Flow: {ConditionType.GetDisplayName()}";
}
