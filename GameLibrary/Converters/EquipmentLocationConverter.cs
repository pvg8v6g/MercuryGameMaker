using System;
using System.Collections.Generic;
using System.Linq;
using GameLibrary.Enumerations;
using GameLibrary.Models.Items;
using Microsoft.UI.Xaml.Data;

namespace GameLibrary.Converters;

public class EquipmentLocationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IEnumerable<Equipment> equipments &&
            parameter is string paramStr &&
            Enum.TryParse<EquipmentLocation>(paramStr, out var location))
        {
            return equipments.Where(e => e.EquipmentLocation == location);
        }
        return value ?? Enumerable.Empty<Equipment>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}