using System.Collections.ObjectModel;
using GameLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.Props;

/// <summary>
/// One line of a fill set: "these props, here, this thick". The props are picked at random from the pool.
/// </summary>
public partial class FillRule : PropertyChangedUpdater
{
    #region Properties

    public string Name
    {
        get;
        set => SetField(ref field, value);
    } = "New Rule";

    // the catalogue props picked from at random
    public ObservableCollection<Guid> Props { get; init; } = [];

    public FillPlace Place
    {
        get;
        set => SetField(ref field, value);
    } = FillPlace.Open;

    // for InsideArea: the name of the map areas to fill
    public string AreaName
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    // only where the ground image is close to this colour (#RRGGBB), empty for any ground (sand, snow, a path)
    public string GroundColor
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    // how far from the ground colour still counts, 0 to 255
    public int ColorTolerance
    {
        get;
        set => SetField(ref field, value);
    } = 40;

    // 0 to 100: how full the place gets, 100 packs it as tight as the spacing allows
    public int Density
    {
        get;
        set => SetField(ref field, value);
    } = 50;

    // the closest two props of this rule stand, in pixels
    public int Spacing
    {
        get;
        set => SetField(ref field, value);
    } = 48;

    // props placed together in a group (flowers), 1 for single props
    public int ClumpSize
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    #endregion
}
