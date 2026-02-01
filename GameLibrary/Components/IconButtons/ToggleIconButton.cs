using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameLibrary.Components.IconButtons;

public class ToggleIconButton : RadioButton
{
    #region Registration

    public static readonly DependencyProperty BitmapSourceProperty = DependencyProperty.Register(
        nameof(BitmapSource), typeof(ImageSource), typeof(ToggleIconButton),
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

    public new CornerRadius CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = value;
            _border.CornerRadius = value;
        }
    }

    private CornerRadius _cornerRadius;

    public Thickness SolidBorderThickness
    {
        get => _solidBorderThickness;
        set
        {
            _solidBorderThickness = value;
            _border.BorderThickness = value;
        }
    }

    private Thickness _solidBorderThickness;

    public Brush SolidBorderBrush
    {
        get => _solidBorderBrush;
        set
        {
            _solidBorderBrush = value;
            _border.BorderBrush = value;
        }
    }

    private Brush _solidBorderBrush = null!;

    #endregion

    #region Fields

    private readonly ImageBrush _imageBrush = new();
    private readonly Border _border = new();

    #endregion

    #region Constructor

    public ToggleIconButton()
    {
        _border.Background = _imageBrush;
        _border.Width = 0;
        _border.Height = 0;
        Content = _border;
    }

    #endregion

    #region Actions & Listeners

    private static void OnBitmapSourcePropertyChanged(DependencyObject @do, DependencyPropertyChangedEventArgs e)
    {
        (@do as ToggleIconButton)?.IconChanged((e.NewValue as ImageSource)!);
    }

    protected virtual void IconChanged(ImageSource n)
    {
        _imageBrush.ImageSource = n;
        if (n is BitmapImage bitmapImage)
        {
            _border.Width = bitmapImage.PixelWidth;
            _border.Height = bitmapImage.PixelHeight;
        }
        else if (n is SvgImageSource svgImageSource)
        {
            _border.Width = svgImageSource.RasterizePixelWidth;
            _border.Height = svgImageSource.RasterizePixelHeight;
        }
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
            // WinUI 3 ToolTipService does not have SetInitialShowDelay directly on the service like WPF
            // It's usually handled via styles or it's just not available in the same way.
        }
    }

    #endregion
}
