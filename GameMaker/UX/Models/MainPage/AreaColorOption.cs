using GameLibrary.Enumerations;
using GameLibrary.Utilities;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// A color an area can be drawn in, with a swatch for the drop down. None is the area type's own color.
/// </summary>
public class AreaColorOption(GameColor? color)
{
    public static AreaColorOption[] All { get; } = [new(null), .. Enum.GetValues<GameColor>().Select(x => new AreaColorOption(x))];

    public GameColor? Color => color;

    public string Name => color?.ToString() ?? "Default";

    public Brush Swatch { get; } = new SolidColorBrush(color?.GetHex()?.ToColor() ?? Microsoft.UI.Colors.Transparent);
}
