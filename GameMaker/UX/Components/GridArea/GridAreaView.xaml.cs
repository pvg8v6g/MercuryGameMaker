using System.Collections.ObjectModel;
using GameLibrary.Models.Areas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace GameMaker.UX.Components.GridArea;

public sealed partial class GridAreaView
{
    public static readonly DependencyProperty HitboxesProperty = DependencyProperty.Register(
        nameof(Hitboxes), typeof(ObservableCollection<Hitbox>), typeof(GridAreaView), new PropertyMetadata(null));

    public ObservableCollection<Hitbox>? Hitboxes
    {
        get => (ObservableCollection<Hitbox>?)GetValue(HitboxesProperty);
        set => SetValue(HitboxesProperty, value);
    }

    private Point _startPoint;
    private bool _isDrawing;

    public GridAreaView()
    {
        InitializeComponent();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Hitboxes == null) return;

        var point = e.GetCurrentPoint(RootGrid).Position;
        _startPoint = point;
        _isDrawing = true;

        PreviewRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(PreviewRectangle, point.X);
        Canvas.SetTop(PreviewRectangle, point.Y);
        PreviewRectangle.Width = 0;
        PreviewRectangle.Height = 0;

        RootGrid.CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;

        var currentPoint = e.GetCurrentPoint(RootGrid).Position;

        var x = Math.Min(_startPoint.X, currentPoint.X);
        var y = Math.Min(_startPoint.Y, currentPoint.Y);
        var width = Math.Abs(_startPoint.X - currentPoint.X);
        var height = Math.Abs(_startPoint.Y - currentPoint.Y);

        Canvas.SetLeft(PreviewRectangle, x);
        Canvas.SetTop(PreviewRectangle, y);
        PreviewRectangle.Width = width;
        PreviewRectangle.Height = height;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;

        _isDrawing = false;
        PreviewRectangle.Visibility = Visibility.Collapsed;
        RootGrid.ReleasePointerCapture(e.Pointer);

        var currentPoint = e.GetCurrentPoint(RootGrid).Position;
        var x = (int)Math.Min(_startPoint.X, currentPoint.X);
        var y = (int)Math.Min(_startPoint.Y, currentPoint.Y);
        var width = (int)Math.Abs(_startPoint.X - currentPoint.X);
        var height = (int)Math.Abs(_startPoint.Y - currentPoint.Y);

        if (width > 0 && height > 0)
        {
            Hitboxes?.Add(new Hitbox
            {
                X = x,
                Y = y,
                Width = width,
                Height = height
            });
        }
    }
}
