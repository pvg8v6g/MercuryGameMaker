using System.Text.Json.Serialization;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Conditions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "@type")]
[JsonDerivedType(typeof(SelfSwitchCondition), "self_switch_condition")]
[JsonDerivedType(typeof(SwitchCondition), "switch_condition")]
[JsonDerivedType(typeof(VariableCondition), "variable_condition")]
[JsonDerivedType(typeof(ItemCondition), "item_condition")]
[JsonDerivedType(typeof(ActorCondition), "actor_condition")]
[JsonDerivedType(typeof(LevelCondition), "level_condition")]
public abstract partial class Condition : PropertyChangedUpdater
{
    #region Properties

    public Guid Guid
    {
        get;
        set => SetField(ref field, value);
    } = Guid.NewGuid();

    #endregion

    #region Overrides

    public abstract override string ToString();

    #endregion
}
