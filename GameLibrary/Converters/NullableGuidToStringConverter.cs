using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

public sealed class NullableGuidToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value is Guid guid ? guid.ToString() : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object parameter, string language)
    {
        if (value is string text && Guid.TryParse(text, out var guid)) return guid;
        return null;
    }
}
