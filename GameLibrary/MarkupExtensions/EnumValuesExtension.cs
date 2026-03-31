using Microsoft.UI.Xaml.Markup;

namespace GameLibrary.MarkupExtensions;

[MarkupExtensionReturnType(ReturnType = typeof(Array))]
public class EnumValuesExtension : MarkupExtension
{
    public Type? EnumType { get; set; }

    protected override object? ProvideValue()
    {
        return EnumType is not { IsEnum: true } ? null : Enum.GetValues(EnumType);
    }
}
