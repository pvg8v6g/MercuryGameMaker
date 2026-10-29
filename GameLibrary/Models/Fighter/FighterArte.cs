using System.Collections.ObjectModel;
using GameLibrary.Models.Conditions;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Fighter;

/*
 * an arte a fighter gains once every condition is met - no conditions means it is always known
 */
public partial class FighterArte : PropertyChangedUpdater
{
    #region Properties

    public Guid? ArteGuid
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<Condition> Conditions
    {
        get;
        set => SetField(ref field, value);
    } = [];

    #endregion
}
