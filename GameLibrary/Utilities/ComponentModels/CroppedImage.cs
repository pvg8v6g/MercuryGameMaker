using Windows.Foundation;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Media;

namespace GameLibrary.Utilities.ComponentModels;

public partial class CroppedImage : PropertyChangedUpdater
{
    public ImageSource? ImageSource
    {
        get;
        set => SetField(ref field, value);
    }

    public Rect? Rect
    {
        get;
        set => SetField(ref field, value);
    } = null;
}
