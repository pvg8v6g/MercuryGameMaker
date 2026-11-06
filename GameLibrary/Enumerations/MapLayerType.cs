using GameLibrary.Attributes;
using MercuryLibrary.Enumerations;

namespace GameLibrary.Enumerations;

// the full map images, each one lives in Graphics\Maps\{type}\map{id}.png
public enum MapLayerType
{
    // below the environments (z -2), what shows through them, like the ocean floor under the water
    [MapLayer(Z = -3)]
    Underlayment,

    [MapLayer(Z = -1)]
    Ground,

    [MapLayer(Z = 100)]
    Parallax,

    [MapLayer(Z = 200, BlendMode = BlendMode.LinearDodge)]
    Still,

    [MapLayer(Z = 200, BlendMode = BlendMode.LinearDodge)]
    Light
}
