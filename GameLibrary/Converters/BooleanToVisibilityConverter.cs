using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        var flag = value as bool? ?? false;
        if (Invert) flag = !flag;
        Console.WriteLine(flag ? Visibility.Visible : Visibility.Collapsed);
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
