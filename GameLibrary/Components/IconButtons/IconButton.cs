using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameLibrary.Components.IconButtons;

public class IconButton : Button
{

    #region Registration

    public static readonly DependencyProperty BitmapSourceProperty = DependencyProperty.Register(
        nameof(BitmapSource), typeof(ImageSource), typeof(IconButton),
        new PropertyMetadata(null, OnBitmapSourcePropertyChanged));

    #endregion

    #region Properties

    public object? ToolTipContent
    {
        get => _toolTipContent;
        set
        {
            _toolTipContent = value;
            InstallToolTip();
        }
    }

    private object? _toolTipContent;

    public int ToolTipInitialDelay { get; set; }

    public ImageSource BitmapSource
    {
        get => (GetValue(BitmapSourceProperty) as ImageSource)!;
        set { SetValue(BitmapSourceProperty, value); }
    }

    #endregion

    #region Fields

    #endregion

    #region Actions & Listeners

    private static void OnBitmapSourcePropertyChanged(DependencyObject @do, DependencyPropertyChangedEventArgs e)
    {
        (@do as IconButton)?.IconChanged((e.NewValue as ImageSource)!);
    }

    protected virtual void IconChanged(ImageSource n)
    {
        var image = new Image { Source = n };
        if (n is BitmapImage bitmapImage)
        {
            image.Width = bitmapImage.PixelWidth;
            image.Height = bitmapImage.PixelHeight;
        }
        else if (n is SvgImageSource svgImageSource)
        {
            image.Width = svgImageSource.RasterizePixelWidth;
            image.Height = svgImageSource.RasterizePixelHeight;
        }
        Content = image;
    }

    #endregion

    #region Functions

    private void InstallToolTip()
    {
        if (ToolTipContent is null)
        {
            ToolTipService.SetToolTip(this, null);
        }
        else
        {
            var toolTip = new ToolTip { Content = ToolTipContent };
            ToolTipService.SetToolTip(this, toolTip);
        }
    }

    #endregion

}
