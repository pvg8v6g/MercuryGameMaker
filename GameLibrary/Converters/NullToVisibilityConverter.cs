using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isNull = value == null;
        bool shouldBeVisible = Invert ? isNull : !isNull;
        return shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
