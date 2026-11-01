using MercuryLibrary.Enumerations;

namespace GameLibrary.Attributes;

/// <summary>
/// The default draw settings for a map layer. A map only stores a layer when it changes one of these.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class MapLayerAttribute : Attribute
{
    #region Fields

    private BlendMode? _blendMode;

    #endregion

    #region Properties

    // draw order, everything on the map (layers and entities) is drawn lowest z first
    public double Z { get; set; }

    public double Opacity { get; set; } = 1.0;

    // attributes can't take a nullable enum, leave this off for a layer that is drawn without a blend mode
    public BlendMode BlendMode
    {
        get => _blendMode ?? default;
        set => _blendMode = value;
    }

    // the blend mode the layer is drawn with, null when none was given
    public BlendMode? MapBlendMode => _blendMode;

    #endregion
}
