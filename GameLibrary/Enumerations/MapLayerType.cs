using GameLibrary.Attributes;
using MercuryLibrary.Enumerations;

namespace GameLibrary.Enumerations;

// the full map images, each one lives in Graphics\Maps\{type}\map{id}.png
public enum MapLayerType
{
    [MapLayer(Z = -2, BlendMode = BlendMode.LinearDodge)]
    Water,

    [MapLayer(Z = -1)]
    Ground,

    [MapLayer(Z = 100)]
    Parallax,

    [MapLayer(Z = 200, BlendMode = BlendMode.LinearDodge)]
    Still,

    [MapLayer(Z = 200, BlendMode = BlendMode.LinearDodge)]
    Light
}
