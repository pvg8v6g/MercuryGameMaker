using Microsoft.UI.Xaml.Shapes;
using System.Collections.ObjectModel;
using GameLibrary.Models.Areas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace GameMaker.UX.Components.GridArea;

public sealed partial class GridAreaView : System.ComponentModel.INotifyPropertyChanged
{
    private static void OnHitboxesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GridAreaView control)
        {
            control.RefreshAnchor();
            if (e.OldValue is ObservableCollection<Hitbox> oldCollection)
                oldCollection.CollectionChanged -= control.OnHitboxesCollectionChanged;
            if (e.NewValue is ObservableCollection<Hitbox> newCollection)
                newCollection.CollectionChanged += control.OnHitboxesCollectionChanged;
        }
    }

    private void OnHitboxesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (Hitbox hitbox in e.NewItems)
            {
                hitbox.AnchorX = AnchorX;
                hitbox.AnchorY = AnchorY;
            }
        }
    }

    public static readonly DependencyProperty HitboxesProperty = DependencyProperty.Register(
        nameof(Hitboxes), typeof(ObservableCollection<Hitbox>), typeof(GridAreaView), new PropertyMetadata(null, OnHitboxesChanged));

    private static void OnCharacterImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GridAreaView control)
        {
            if (e.OldValue is System.ComponentModel.INotifyPropertyChanged oldImage)
                oldImage.PropertyChanged -= control.OnCharacterImageSubPropertyChanged;
            if (e.NewValue is System.ComponentModel.INotifyPropertyChanged newImage)
                newImage.PropertyChanged += control.OnCharacterImageSubPropertyChanged;
            control.RefreshAnchor();
        }
    }

    private void OnCharacterImageSubPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Rect")
        {
            RefreshAnchor();
        }
    }

    public static readonly DependencyProperty CharacterImageProperty = DependencyProperty.Register(
        nameof(CharacterImage), typeof(GameLibrary.Utilities.ComponentModels.CroppedImage), typeof(GridAreaView), new PropertyMetadata(null, OnCharacterImageChanged));

    public GameLibrary.Utilities.ComponentModels.CroppedImage? CharacterImage
    {
        get => (GameLibrary.Utilities.ComponentModels.CroppedImage?) GetValue(CharacterImageProperty);
        set
        {
            SetValue(CharacterImageProperty, value);
            RefreshAnchor();
        }
    }

    public ObservableCollection<Hitbox>? Hitboxes
    {
        get => (ObservableCollection<Hitbox>?) GetValue(HitboxesProperty);
        set => SetValue(HitboxesProperty, value);
    }

    public static readonly DependencyProperty GridSizeProperty = DependencyProperty.Register(
        nameof(GridSize), typeof(int), typeof(GridAreaView), new PropertyMetadata(21));

    public int GridSize
    {
        get => (int) GetValue(GridSizeProperty);
        set
        {
            SetValue(GridSizeProperty, value);
            OnPropertyChanged(nameof(GridSizeText));
            OnPropertyChanged(nameof(ActualWidth));
            OnPropertyChanged(nameof(ActualHeight));
            RefreshAnchor();
            UpdateGridLines();
        }
    }

    public static readonly DependencyProperty BoxSizeProperty = DependencyProperty.Register(
        nameof(BoxSize), typeof(int), typeof(GridAreaView), new PropertyMetadata(48));

    public int BoxSize
    {
        get => (int) GetValue(BoxSizeProperty);
        set
        {
            SetValue(BoxSizeProperty, value);
            OnPropertyChanged(nameof(BoxSizeText));
            OnPropertyChanged(nameof(ActualWidth));
            OnPropertyChanged(nameof(ActualHeight));
            RefreshAnchor();
            UpdateGridLines();
        }
    }

    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(GridAreaView), new PropertyMetadata(true));

    public bool ShowGrid
    {
        get => (bool) GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public static readonly DependencyProperty GridSizeTextProperty = DependencyProperty.Register(
        nameof(GridSizeText), typeof(string), typeof(GridAreaView), new PropertyMetadata("21"));

    public string GridSizeText
    {
        get => (string) GetValue(GridSizeTextProperty);
        set
        {
            SetValue(GridSizeTextProperty, value);
            if (int.TryParse(value, out var result))
            {
                GridSize = result;
            }
        }
    }

    public static readonly DependencyProperty BoxSizeTextProperty = DependencyProperty.Register(
        nameof(BoxSizeText), typeof(string), typeof(GridAreaView), new PropertyMetadata("32"));

    public string BoxSizeText
    {
        get => (string) GetValue(BoxSizeTextProperty);
        set
        {
            SetValue(BoxSizeTextProperty, value);
            if (int.TryParse(value, out var result))
            {
                BoxSize = result;
            }
        }
    }

    public static readonly DependencyProperty InputTypeProperty = DependencyProperty.Register(
        "InputType", typeof(GameLibrary.Enumerations.InputType), typeof(GridAreaView),
        new PropertyMetadata(GameLibrary.Enumerations.InputType.Integer));

    public GameLibrary.Enumerations.InputType InputType
    {
        get => (GameLibrary.Enumerations.InputType) GetValue(InputTypeProperty);
        set => SetValue(InputTypeProperty, value);
    }

    private Point _startPoint;
    private bool _isDrawing;

    public int AnchorX
    {
        get
        {
            var width = ActualWidth;
            var spriteWidth = CharacterImage?.Rect?.Width ?? 0;
            var w = width / 2.0 - spriteWidth / 2.0;
            var res = (int) RoundToMultiple(w, BoxSize, Rounding.Down);
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] AnchorX: {res}, Width: {width}, SpriteWidth: {spriteWidth}, BoxSize: {BoxSize}");
            return res;
        }
    }

    public int AnchorY
    {
        get
        {
            var height = ActualHeight;
            var spriteHeight = CharacterImage?.Rect?.Height ?? 0;
            var h = height / 2.0 - spriteHeight / 2.0;
            var res = (int) RoundToMultiple(h, BoxSize, Rounding.Down);
            System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] AnchorY: {res}, Height: {height}, SpriteHeight: {spriteHeight}, BoxSize: {BoxSize}");
            return res;
        }
    }

    public GridAreaView()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateGridLines();
    }

    private void UpdateGridLines()
    {
        if (GridLinesCanvas == null) return;

        GridLinesCanvas.Children.Clear();

        if (BoxSize <= 0 || GridSize <= 0) return;

        var strokeColor = (Microsoft.UI.Xaml.Media.SolidColorBrush) Application.Current.Resources["MediumGreyBrush"];
        var anchorStrokeColor = (Microsoft.UI.Xaml.Media.SolidColorBrush) Application.Current.Resources["SecondaryBrush"];
        var width = ActualWidth;
        var height = ActualHeight;

        var currentAnchorX = AnchorX;
        var currentAnchorY = AnchorY;

        for (int x = 0; x <= width; x += BoxSize)
        {
            var line = new Line
            {
                X1 = x,
                Y1 = 0,
                X2 = x,
                Y2 = height,
                Stroke = x == currentAnchorX ? anchorStrokeColor : strokeColor,
                StrokeThickness = x == currentAnchorX ? 1.5 : 0.5
            };
            GridLinesCanvas.Children.Add(line);
        }

        for (int y = 0; y <= height; y += BoxSize)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = width,
                Y2 = y,
                Stroke = y == currentAnchorY ? anchorStrokeColor : strokeColor,
                StrokeThickness = y == currentAnchorY ? 1.5 : 0.5
            };
            GridLinesCanvas.Children.Add(line);
        }
    }

    private void OnHitboxPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Rectangle { DataContext: Hitbox hitbox })
        {
            var properties = e.GetCurrentPoint(RootGrid).Properties;
            if (properties.IsRightButtonPressed)
            {
                Hitboxes?.Remove(hitbox);
                e.Handled = true;
            }
        }
    }

    private void OnClearAllClick(object sender, RoutedEventArgs e)
    {
        Hitboxes?.Clear();
    }

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }

    public new int ActualWidth => GridSize * BoxSize;
    public new int ActualHeight => GridSize * BoxSize;

    private void RefreshAnchor()
    {
        OnPropertyChanged(nameof(AnchorX));
        OnPropertyChanged(nameof(AnchorY));

        if (GridLinesCanvas != null) UpdateGridLines();

        if (Hitboxes != null)
        {
            foreach (var hitbox in Hitboxes)
            {
                hitbox.AnchorX = AnchorX;
                hitbox.AnchorY = AnchorY;
            }
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    private enum Rounding { Up, Down }

    private double RoundToMultiple(double value, int multiple, Rounding rounding)
    {
        if (rounding == Rounding.Up)
            return Math.Ceiling(value / multiple) * multiple;
        return Math.Floor(value / multiple) * multiple;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Hitboxes == null) return;

        var properties = e.GetCurrentPoint(RootGrid).Properties;
        if (properties.IsRightButtonPressed) return;

        var point = e.GetCurrentPoint(RootGrid).Position;
        if (point.X < 0 || point.Y < 0 || point.X > ActualWidth || point.Y > ActualHeight) return;

        var snappedX = RoundToMultiple(point.X, BoxSize, Rounding.Down);
        var snappedY = RoundToMultiple(point.Y, BoxSize, Rounding.Down);

        _startPoint = new Point(snappedX, snappedY);
        _isDrawing = true;

        PreviewRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(PreviewRectangle, snappedX);
        Canvas.SetTop(PreviewRectangle, snappedY);
        PreviewRectangle.Width = BoxSize;
        PreviewRectangle.Height = BoxSize;

        RootGrid.CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;

        var point = e.GetCurrentPoint(RootGrid).Position;

        if (point.X < 0) point.X = 0;
        if (point.Y < 0) point.Y = 0;
        if (point.X > ActualWidth) point.X = ActualWidth;
        if (point.Y > ActualHeight) point.Y = ActualHeight;

        var x = Math.Min(RoundToMultiple(point.X, BoxSize, Rounding.Down), _startPoint.X);
        var y = Math.Min(RoundToMultiple(point.Y, BoxSize, Rounding.Down), _startPoint.Y);

        var w = Math.Abs(RoundToMultiple(point.X, BoxSize, Rounding.Down) - _startPoint.X) + BoxSize;
        var h = Math.Abs(RoundToMultiple(point.Y, BoxSize, Rounding.Down) - _startPoint.Y) + BoxSize;

        Canvas.SetLeft(PreviewRectangle, x);
        Canvas.SetTop(PreviewRectangle, y);
        PreviewRectangle.Width = w;
        PreviewRectangle.Height = h;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing) return;

        _isDrawing = false;
        PreviewRectangle.Visibility = Visibility.Collapsed;
        RootGrid.ReleasePointerCapture(e.Pointer);

        var x = Canvas.GetLeft(PreviewRectangle);
        var y = Canvas.GetTop(PreviewRectangle);
        var width = PreviewRectangle.Width;
        var height = PreviewRectangle.Height;

        var currentAnchorX = AnchorX;
        var currentAnchorY = AnchorY;

        var hitbox = new Hitbox
        {
            X = (int)(x - currentAnchorX),
            Y = (int)(y - currentAnchorY),
            Width = (int)width,
            Height = (int)height,
            AnchorX = currentAnchorX,
            AnchorY = currentAnchorY
        };
        Hitboxes?.Add(hitbox);

        System.Diagnostics.Debug.WriteLine($"[DEBUG_LOG] Added Hitbox: X={hitbox.X}, Y={hitbox.Y}, W={hitbox.Width}, H={hitbox.Height}, AnchorX={hitbox.AnchorX}, AnchorY={hitbox.AnchorY}");
    }
}
