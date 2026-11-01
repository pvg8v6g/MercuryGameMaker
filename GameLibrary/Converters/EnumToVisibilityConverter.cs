using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

public sealed class EnumToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        var match = value?.ToString() == parameter.ToString();
        if (Invert) match = !match;
        return match ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
