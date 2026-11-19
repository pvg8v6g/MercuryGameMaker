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

    // the target tinted on some frames (a poison animation turning the character green), the frame order doesn't matter
    public ObservableCollection<AnimationTint> TargetTints
    {
        get;
        set => SetField(ref field, value);
    } = [];

    // shockwaves sent out from the target, each starting on its frame
    public ObservableCollection<AnimationShockwave> Shockwaves
    {
        get;
        set => SetField(ref field, value);
    } = [];

    // heat shimmering around the target, each starting on its frame
    public ObservableCollection<AnimationHeatHaze> HeatHazes
    {
        get;
        set => SetField(ref field, value);
    } = [];

    // the last frame anything shows on: a cell, a tint, or a shockwave or heat still going
    public int LastFrame()
    {
        return Effects.Select(x => x.Frame)
            .Concat(TargetTints.Select(x => x.Frame))
            .Concat(Shockwaves.Select(x => x.LastFrame))
            .Concat(HeatHazes.Select(x => x.LastFrame))
            .DefaultIfEmpty(0)
            .Max();
    }

    #endregion
}
