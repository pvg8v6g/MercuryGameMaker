using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace GameLibrary.Utilities;

/// <summary>
/// Helpers for interpreting the hex color strings stored in enum <see cref="DisplayAttribute"/>
/// names (e.g. <c>GameColor</c>, <c>ActionColor</c>) as WinUI colors and brushes.
/// </summary>
public static class ColorExtensions
{
    /// <summary>Reads the hex string from an enum value's <see cref="DisplayAttribute.Name"/>.</summary>
    public static string? GetHex(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name;
    }

    /// <summary>Parses a <c>#RRGGBB</c> or <c>#AARRGGBB</c> hex string into a <see cref="Color"/>.</summary>
    public static Color ToColor(this string hex)
    {
        var value = hex.TrimStart('#');

        byte a = 255;
        var offset = 0;
        if (value.Length == 8)
        {
            a = Convert.ToByte(value.Substring(0, 2), 16);
            offset = 2;
        }

        var r = Convert.ToByte(value.Substring(offset, 2), 16);
        var g = Convert.ToByte(value.Substring(offset + 2, 2), 16);
        var b = Convert.ToByte(value.Substring(offset + 4, 2), 16);

        return ColorHelper.FromArgb(a, r, g, b);
    }

    /// <summary>Resolves an enum value's hex <see cref="DisplayAttribute"/> name to a solid brush.</summary>
    public static Brush ToBrush(this Enum value)
    {
        var hex = value.GetHex();
        return new SolidColorBrush(hex?.ToColor() ?? Colors.Transparent);
    }

    /// <summary>
    /// Multiplies the RGB channels of a color by <paramref name="factor"/> (0..1) to darken it,
    /// so the lighter accent colors stay legible against a light background.
    /// </summary>
    public static Color Darken(this Color color, double factor = 0.5)
    {
        factor = Math.Clamp(factor, 0, 1);
        return ColorHelper.FromArgb(
            color.A,
            (byte) (color.R * factor),
            (byte) (color.G * factor),
            (byte) (color.B * factor));
    }

    /// <summary>Resolves an enum value's hex color to a darkened solid brush (see <see cref="Darken"/>).</summary>
    public static Brush ToDarkenedBrush(this Enum value, double factor = 0.5)
    {
        var hex = value.GetHex();
        return new SolidColorBrush(hex is null ? Colors.Transparent : hex.ToColor().Darken(factor));
    }
}
