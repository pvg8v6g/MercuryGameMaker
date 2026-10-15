using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

public partial class NullToInvalidIndexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value ?? -1;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is -1 ? null : value;
    }
}
