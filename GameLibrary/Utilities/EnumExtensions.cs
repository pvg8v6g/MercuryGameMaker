using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace GameLibrary.Utilities;

/// <summary>Helpers for reading metadata off enum values.</summary>
public static class EnumExtensions
{
    /// <summary>
    /// Returns the enum value's <see cref="DisplayAttribute.Name"/> when present, otherwise
    /// the raw enum name. Useful for rendering friendly text in action summaries.
    /// </summary>
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }
}
