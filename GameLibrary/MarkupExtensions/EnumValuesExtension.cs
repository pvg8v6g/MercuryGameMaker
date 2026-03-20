using Microsoft.UI.Xaml.Markup;

namespace GameLibrary.MarkupExtensions;

[MarkupExtensionReturnType(ReturnType = typeof(Array))]
public class EnumValuesExtension : MarkupExtension
{
    public Type EnumType { get; set; }

    protected override object ProvideValue()
    {
        if (EnumType == null || !EnumType.IsEnum)
            return null;

        return Enum.GetValues(EnumType);
    }
}
