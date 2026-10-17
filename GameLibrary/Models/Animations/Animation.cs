using System.Collections.ObjectModel;

namespace GameLibrary.Models.Animations;

public partial class Animation : BaseModel
{
    #region Properties

    public ObservableCollection<AnimationEffect> Effects
    {
        get;
        set => SetField(ref field, value);
    } = [];

    #endregion
}
