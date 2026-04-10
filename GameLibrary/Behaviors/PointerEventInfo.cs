using Microsoft.UI.Xaml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace GameLibrary.Behaviors;

public sealed record PointerEventInfo(UIElement Sender, PointerRoutedEventArgs Args)
{
    public Point Position => Args.GetCurrentPoint(Sender).Position;

    public PointerPointProperties Properties => Args.GetCurrentPoint(Sender).Properties;

    public PointerUpdateKind UpdateKind => Args.GetCurrentPoint(Sender).Properties.PointerUpdateKind;
}
