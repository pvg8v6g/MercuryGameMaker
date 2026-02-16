using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

public class NullToBooleanConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isNull = value == null;
        return Invert ? isNull : !isNull;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
