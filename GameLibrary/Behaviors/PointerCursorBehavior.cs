using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Reflection;

namespace GameLibrary.Behaviors;

public class PointerCursorBehavior
{
    public static readonly DependencyProperty CursorTypeProperty = DependencyProperty.RegisterAttached(
        "CursorType", typeof(string), typeof(PointerCursorBehavior), new PropertyMetadata(null, OnCursorTypeChanged));

    public static void SetCursorType(DependencyObject element, string value)
    {
        element.SetValue(CursorTypeProperty, value);
    }

    public static string GetCursorType(DependencyObject element)
    {
        return (string)element.GetValue(CursorTypeProperty);
    }

    private static void OnCursorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && e.NewValue is string cursorTypeStr)
        {
            InputCursor? cursor = null;
            if (cursorTypeStr == "Hand")
            {
                cursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            }
            else if (cursorTypeStr == "Arrow")
            {
                cursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }

            if (cursor != null)
            {
                // Use reflection to set ProtectedCursor because it's protected
                var prop = typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                prop?.SetValue(element, cursor);
            }
        }
    }
}
