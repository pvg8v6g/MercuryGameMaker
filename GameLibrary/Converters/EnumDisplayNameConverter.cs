using GameLibrary.Utilities;
using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

// an enum value's Display name, or its own name when it has none
public partial class EnumDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is Enum enumValue ? enumValue.GetDisplayName() : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
