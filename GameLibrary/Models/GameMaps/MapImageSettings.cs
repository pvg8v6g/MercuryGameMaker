using System.Text.Json.Serialization;
using GameLibrary.Attributes;
using MercuryLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameLibrary.Models.GameMaps;

/// <summary>
/// How a full map image is drawn. The image itself is never stored, only these settings.
/// </summary>
public abstract partial class MapImageSettings : PropertyChangedUpdater
{
    #region Properties

    // draw order, everything on the map (images and entities) is drawn lowest z first
    public double Z
    {
        get;
        set => SetField(ref field, value);
    }

    public double Opacity
    {
        get;
        set => SetField(ref field, value);
    }

    public BlendMode? MapBlendMode
    {
        get;
        set => SetField(ref field, value);
    }

    // what Reset goes back to
    [JsonIgnore]
    public abstract MapLayerAttribute DefaultSettings { get; }

    #endregion

    #region Methods

    public void Reset()
    {
        var defaults = DefaultSettings;
        Z = defaults.Z;
        Opacity = defaults.Opacity;
        MapBlendMode = defaults.MapBlendMode;
    }

    #endregion
}
