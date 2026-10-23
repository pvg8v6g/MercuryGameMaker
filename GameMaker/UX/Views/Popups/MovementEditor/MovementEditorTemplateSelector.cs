using GameLibrary.Models.Movements;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Views.Popups.MovementEditor;

public partial class MovementEditorTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Move { get; set; }

    public DataTemplate? MoveToward { get; set; }

    public DataTemplate? MoveAway { get; set; }

    public DataTemplate? Speed { get; set; }

    public DataTemplate? Opacity { get; set; }

    public DataTemplate? Wait { get; set; }

    public DataTemplate? Turn { get; set; }

    public DataTemplate? Blend { get; set; }

    public DataTemplate? CharacterImage { get; set; }

    public DataTemplate? ActivateArte { get; set; }

    public DataTemplate? Empty { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        return item switch
        {
            MoveMovement => Move,
            MoveTowardMovement => MoveToward,
            MoveAwayMovement => MoveAway,
            SpeedMovement => Speed,
            OpacityMovement => Opacity,
            WaitMovement => Wait,
            TurnMovement => Turn,
            BlendModeMovement => Blend,
            CharacterImageMovement => CharacterImage,
            ActivateArteMovement => ActivateArte,
            _ => Empty
        };
    }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
