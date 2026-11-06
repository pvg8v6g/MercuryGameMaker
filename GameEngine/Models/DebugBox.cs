using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using MercuryLibrary.WinUI3Components;
using Windows.UI;

namespace GameEngine.Models;

/// <summary>
/// Where a debug box goes, in scaled map pixels. Worked out on the game loop thread, which can't touch brushes.
/// </summary>
public readonly record struct DebugShape(double X, double Y, double Width, double Height, Color Color);

/// <summary>
/// A box drawn over the game to show a hitbox or an area. Made on the ui thread, it holds brushes.
/// </summary>
public partial class DebugBox(DebugShape shape) : PropertyChangedUpdater
{
    #region Constants

    private const byte FillAlpha = 90;

    #endregion

    #region Properties

    public Color Color => shape.Color;

    public double X
    {
        get;
        private set => SetField(ref field, value);
    } = shape.X;

    public double Y
    {
        get;
        private set => SetField(ref field, value);
    } = shape.Y;

    public double Width
    {
        get;
        private set => SetField(ref field, value);
    } = shape.Width;

    public double Height
    {
        get;
        private set => SetField(ref field, value);
    } = shape.Height;

    public Brush Fill { get; } = new SolidColorBrush(ColorHelper.FromArgb(FillAlpha, shape.Color.R, shape.Color.G, shape.Color.B));

    public Brush Stroke { get; } = new SolidColorBrush(shape.Color);

    #endregion

    #region Methods

    public void Move(DebugShape moved)
    {
        X = moved.X;
        Y = moved.Y;
        Width = moved.Width;
        Height = moved.Height;
    }

    #endregion
}
