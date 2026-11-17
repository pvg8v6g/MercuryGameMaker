using System.Collections.ObjectModel;
using GameLibrary.Models.Areas;

namespace GameLibrary.Models.Props;

/// <summary>
/// One entry of the props catalogue (Props\{guid}.data): a tree, rock, ruin arch and so on that can be placed on maps.
/// Everything here is set once and every placed copy of the prop uses it.
/// </summary>
public partial class Prop : BaseModel
{
    #region Properties

    // the picture, in Graphics\Props
    public string? ImageName
    {
        get;
        set => SetField(ref field, value);
    }

    // what blocks movement (a tree's trunk, not its leaves), offsets from the picture's top left like a character's hitboxes
    public ObservableCollection<Area> Footprint { get; init; } = [];

    // characters sort against the prop at the bottom of its picture, this moves that line up (negative) or down
    public int SortOffset
    {
        get;
        set => SetField(ref field, value);
    }

    // how much of the bottom of the picture is in the ground (roots, the base of a rock), in pixels
    public int BuryDepth
    {
        get;
        set => SetField(ref field, value);
    }

    // how far above the ground line the picture fades in, so it blends into the grass instead of a hard edge
    public int BuryFeather
    {
        get;
        set => SetField(ref field, value);
    } = 6;

    // flat props (rugs, puddles, floor stones) are always drawn under the characters and never sorted
    public bool Flat
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion
}
