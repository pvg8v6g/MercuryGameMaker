using System.Text.Json.Serialization;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Actions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "@type")]
[JsonDerivedType(typeof(ActorArteAction), "actor_arte_action")]
[JsonDerivedType(typeof(ActorAttributeAction), "actor_attribute_action")]
[JsonDerivedType(typeof(ActorDescriptionAction), "actor_description_action")]
[JsonDerivedType(typeof(ActorEquipmentAction), "actor_equipment_action")]
[JsonDerivedType(typeof(ActorExperienceAction), "actor_experience_action")]
[JsonDerivedType(typeof(ActorImageAction), "actor_image_action")]
[JsonDerivedType(typeof(ActorLevelAction), "actor_level_action")]
[JsonDerivedType(typeof(ActorLifeAction), "actor_life_action")]
[JsonDerivedType(typeof(ActorManaAction), "actor_mana_action")]
[JsonDerivedType(typeof(ActorNameAction), "actor_name_action")]
[JsonDerivedType(typeof(ActorStateAction), "actor_state_action")]
[JsonDerivedType(typeof(BranchAction), "branch_action")]
[JsonDerivedType(typeof(ChangeItemAction), "change_item_action")]
[JsonDerivedType(typeof(EntityAnimationAction), "entity_animation_action")]
[JsonDerivedType(typeof(EntityBalloonIconAction), "entity_balloon_icon_action")]
[JsonDerivedType(typeof(EntityEraseAction), "entity_erase_action")]
[JsonDerivedType(typeof(EntityMovementAction), "entity_movement_action")]
[JsonDerivedType(typeof(EntityOpacityAction), "entity_opacity_action")]
[JsonDerivedType(typeof(EntityStateAction), "entity_state_action")]
[JsonDerivedType(typeof(EntityTransferAction), "entity_transfer_action")]
[JsonDerivedType(typeof(ExitProcessingAction), "exit_processing_action")]
[JsonDerivedType(typeof(FadeMediaAction), "fade_media_action")]
[JsonDerivedType(typeof(FlowAction), "flow_action")]
[JsonDerivedType(typeof(GameOverAction), "game_over_action")]
[JsonDerivedType(typeof(InitiateBattleAction), "initiate_battle_action")]
[JsonDerivedType(typeof(InputNumberAction), "input_number_action")]
[JsonDerivedType(typeof(MenuAccessAction), "menu_access_action")]
[JsonDerivedType(typeof(MenuAction), "menu_action")]
[JsonDerivedType(typeof(MoneyAction), "money_action")]
[JsonDerivedType(typeof(PartyAction), "party_action")]
[JsonDerivedType(typeof(PlayMediaAction), "play_media_action")]
[JsonDerivedType(typeof(RecoverAction), "recovery_action")]
[JsonDerivedType(typeof(SaveAction), "save_action")]
[JsonDerivedType(typeof(ScreenFadeAction), "screen_fade_action")]
[JsonDerivedType(typeof(ScrollMapAction), "scroll_map_action")]
[JsonDerivedType(typeof(SelectItemAction), "select_item_action")]
[JsonDerivedType(typeof(ShopAction), "shop_action")]
[JsonDerivedType(typeof(ShowChoicesAction), "show_choices_action")]
[JsonDerivedType(typeof(ShowTextAction), "show_text_action")]
[JsonDerivedType(typeof(WaitAction), "wait_action")]
[JsonDerivedType(typeof(WeatherAction), "weather_action")]
public abstract partial class PageAction : PropertyChangedUpdater
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
