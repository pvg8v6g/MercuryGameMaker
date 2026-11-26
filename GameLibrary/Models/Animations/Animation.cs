using System.Collections.ObjectModel;

namespace GameLibrary.Models.Animations;

public partial class Animation : BaseModel
{
    #region Properties

    // drawn behind every character and prop, over the ground, so busy battles aren't buried under effects
    public bool PlaysBehind
    {
        get;
        set => SetField(ref field, value);
    }

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

    // lightning drawn from code, each starting on its frame
    public ObservableCollection<AnimationLightning> Lightnings
    {
        get;
        set => SetField(ref field, value);
    } = [];

    // sounds played on their frame, an animation can be only sounds
    public ObservableCollection<AnimationSound> Sounds
    {
        get;
        set => SetField(ref field, value);
    } = [];

    // the last frame anything happens on: a cell, a tint, a sound, or a shockwave, heat or lightning still going
    public int LastFrame()
    {
        return Effects.Select(x => x.Frame)
            .Concat(TargetTints.Select(x => x.Frame))
            .Concat(Sounds.Select(x => x.Frame))
            .Concat(Shockwaves.Select(x => x.LastFrame))
            .Concat(HeatHazes.Select(x => x.LastFrame))
            .Concat(Lightnings.Select(x => x.LastFrame))
            .DefaultIfEmpty(0)
            .Max();
    }

    #endregion
}
