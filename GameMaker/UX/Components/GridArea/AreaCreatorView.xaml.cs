using System.Collections.ObjectModel;
using System.Drawing;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Utilities.Calculations;
using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace GameMaker.UX.Components.GridArea;

public partial class AreaCreatorView
{
    #region Registered Dependencies

    public static readonly DependencyProperty HitboxesProperty = DependencyProperty.Register(
        nameof(Hitboxes), typeof(ObservableCollection<Area>), typeof(AreaCreatorView), new PropertyMetadata(null));

    public ObservableCollection<Area>? Hitboxes
    {
        get => (ObservableCollection<Area>?) GetValue(HitboxesProperty);
        set => SetValue(HitboxesProperty, value);
    }

    public static readonly DependencyProperty CharacterImageProperty = DependencyProperty.Register(nameof(CharacterImage), typeof(CroppedImage),
        typeof(AreaCreatorView), new PropertyMetadata(null, OnCharacterImageChanged));

    public CroppedImage? CharacterImage
    {
        get => (CroppedImage?) GetValue(CharacterImageProperty);
        set => SetValue(CharacterImageProperty, value);
    }

    public static readonly DependencyProperty ShowAnchorProperty = DependencyProperty.Register(nameof(ShowAnchor), typeof(bool),
        typeof(AreaCreatorView), new PropertyMetadata(false, OnShowAnchorChanged));

    public bool ShowAnchor
    {
        get => (bool) GetValue(ShowAnchorProperty);
        set => SetValue(ShowAnchorProperty, value);
    }

    // the character stands on the anchor tile (feet on it, the picture rising above, centred) instead of the picture's
    // top left sitting on it. Artes are drawn from the tile their user stands on
    public static readonly DependencyProperty StandOnAnchorProperty = DependencyProperty.Register(nameof(StandOnAnchor), typeof(bool),
        typeof(AreaCreatorView), new PropertyMetadata(false, OnShowAnchorChanged));

    public bool StandOnAnchor
    {
        get => (bool) GetValue(StandOnAnchorProperty);
        set => SetValue(StandOnAnchorProperty, value);
    }

    // the size of the tile the character stands on (game pixels), kept whatever the grid is divided into
    public static readonly DependencyProperty AnchorTileSizeProperty = DependencyProperty.Register(nameof(AnchorTileSize), typeof(int),
        typeof(AreaCreatorView), new PropertyMetadata(48, OnShowAnchorChanged));

    public int AnchorTileSize
    {
        get => (int) GetValue(AnchorTileSizeProperty);
        set => SetValue(AnchorTileSizeProperty, value);
    }

    // the character's own hitboxes (offsets from the picture's top left), shown as outlines that can't be edited here
    public static readonly DependencyProperty CharacterHitboxesProperty = DependencyProperty.Register(nameof(CharacterHitboxes),
        typeof(ObservableCollection<Area>), typeof(AreaCreatorView), new PropertyMetadata(null));

    public ObservableCollection<Area>? CharacterHitboxes
    {
        get => (ObservableCollection<Area>?) GetValue(CharacterHitboxesProperty);
        set => SetValue(CharacterHitboxesProperty, value);
    }

    #endregion

    #region Internal State

    private static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(AreaCreatorState), typeof(AreaCreatorView), new PropertyMetadata(null));

    private AreaCreatorState State
    {
        get => (AreaCreatorState) GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    #endregion

    #region Constructor

    public AreaCreatorView()
    {
        InitializeComponent();
        State = new AreaCreatorState(OnGridMetricsChanged);
    }

    #endregion

    #region Event Handlers

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadAnchor();
        RebuildGridLines();
    }

    private void OnClearAllClicked(object sender, RoutedEventArgs e)
    {
        Hitboxes?.Clear();
    }

    private void OnHitboxPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint((UIElement) sender);
        if (!point.Properties.IsRightButtonPressed) return;

        if (sender is FrameworkElement { DataContext: Area area })
        {
            Hitboxes?.Remove(area);
            e.Handled = true;
        }
    }

    private void OnRootGridPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            element.CapturePointer(e.Pointer);
        }

        var point = e.GetCurrentPoint(RootGrid);
        if (!point.Properties.IsLeftButtonPressed) return;

        var pos = point.Position;
        var x = Calculations.RoundToMultiple(pos.X, State.BoxSize, Rounding.Down);
        var y = Calculations.RoundToMultiple(pos.Y, State.BoxSize, Rounding.Down);
        State.StartPosition = new Point(x, y);
        State.PreviewPosition = new Position
            { X = State.StartPosition.Value.X, Y = State.StartPosition.Value.Y, Width = State.BoxSize, Height = State.BoxSize };
    }

    private void OnRootGridPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(RootGrid);
        if (State.StartPosition is null || !point.Properties.IsLeftButtonPressed || State.PreviewPosition is null) return;

        var pos = point.Position;
        if (pos.X < 0) pos.X = 0;
        if (pos.Y < 0) pos.Y = 0;
        if (pos.X > State.InternalGridSize) pos.X = State.InternalGridSize;
        if (pos.Y > State.InternalGridSize) pos.Y = State.InternalGridSize;

        var roundDirectionX = pos.X > State.StartPosition.Value.X ? Rounding.Up : Rounding.Down;
        var roundDirectionY = pos.Y > State.StartPosition.Value.Y ? Rounding.Up : Rounding.Down;

        var x = Math.Min(Calculations.RoundToMultiple(pos.X, State.BoxSize, roundDirectionX), State.StartPosition.Value.X);
        var y = Math.Min(Calculations.RoundToMultiple(pos.Y, State.BoxSize, roundDirectionY), State.StartPosition.Value.Y);

        var extraSizingW = pos.X >= State.StartPosition.Value.X ? 0 : State.BoxSize;
        var extraSizingH = pos.Y >= State.StartPosition.Value.Y ? 0 : State.BoxSize;

        var w = Math.Max(State.BoxSize,
            Math.Max(Calculations.RoundToMultiple(pos.X, State.BoxSize, roundDirectionX), State.StartPosition.Value.X) - x) + extraSizingW;
        var h = Math.Max(State.BoxSize,
            Math.Max(Calculations.RoundToMultiple(pos.Y, State.BoxSize, roundDirectionY), State.StartPosition.Value.Y) - y) + extraSizingH;
        State.PreviewPosition.X = x;
        State.PreviewPosition.Y = y;
        State.PreviewPosition.Width = w;
        State.PreviewPosition.Height = h;
    }

    private void OnRootGridPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement element)
        {
            element.ReleasePointerCapture(e.Pointer);
        }

        if (State.PreviewPosition is null) return;

        var point = e.GetCurrentPoint(RootGrid);
        if (point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased) return;

        State.StartPosition = null;
        var x = State.AnchorPosition is null ? State.PreviewPosition.X : State.PreviewPosition.X - State.AnchorPosition.X;
        var y = State.AnchorPosition is null ? State.PreviewPosition.Y : State.PreviewPosition.Y - State.AnchorPosition.Y;
        var area = new Area
        {
            OffsetX = x,
            OffsetY = y,
            Width = State.PreviewPosition.Width,
            Height = State.PreviewPosition.Height
        };
        Hitboxes?.Add(area);
    }

    #endregion

    #region Listeners

    private static void OnCharacterImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AreaCreatorView control) return;
        control.LoadAnchor();
        control.Bindings.Update();
    }

    private static void OnShowAnchorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AreaCreatorView control) return;
        control.LoadAnchor();
        control.Bindings.Update();
    }

    #endregion

    #region Private Methods

    private void LoadAnchor()
    {
        if (StandOnAnchor)
        {
            // the anchor is a whole game tile whatever the grid is divided into, and the picture stands on it the way
            // the game draws a character on its tile, so the character's hitbox lands on the same grid lines
            var tile = AnchorTileSize;
            var center = Calculations.RoundToMultiple(State.InternalGridSize / 2.0, State.BoxSize, Rounding.Down);
            State.AnchorSize = tile;
            State.AnchorPosition = new Position { X = center, Y = center };
            var size = CharacterImage?.Rect;
            var width = size?.Width ?? tile;
            var height = size?.Height ?? tile;
            State.SpritePosition = new Position
            {
                X = center + (int) Math.Round((tile - width) / 2.0),
                Y = center + (int) Math.Round(tile - height)
            };
            return;
        }

        State.AnchorSize = 0;
        LoadTopLeftAnchor();
        State.SpritePosition = State.AnchorPosition;
    }

    private void LoadTopLeftAnchor()
    {
        if (CharacterImage?.Rect is not null)
        {
            // center in whole blocks so an even sprite on an even grid gets equal space on both sides
            var x = CenteredBlockOffset(CharacterImage.Rect.Value.Width);
            var y = CenteredBlockOffset(CharacterImage.Rect.Value.Height);
            State.AnchorPosition = new Position { X = x, Y = y };
            return;
        }

        if (ShowAnchor)
        {
            var center = Calculations.RoundToMultiple(State.InternalGridSize / 2.0, State.BoxSize, Rounding.Down);
            State.AnchorPosition = new Position { X = center, Y = center };
            return;
        }

        State.AnchorPosition = null;
    }

    // pixel offset that leaves the same number of blank blocks on each side (odd leftovers go to the far side)
    private int CenteredBlockOffset(double spriteSize)
    {
        if (State.BoxSize <= 0) return 0;

        // the half pixel tolerance keeps frames like 48.08px from counting as an extra block
        var spriteBlocks = Math.Max(1, (int) Math.Ceiling((spriteSize - 0.5) / State.BoxSize));
        var blankBlocks = State.GridSize - spriteBlocks;
        return (int) Math.Floor(blankBlocks / 2.0) * State.BoxSize;
    }

    private void RebuildGridLines()
    {
        State.GridLines.Clear();

        if (State.BoxSize <= 0 || State.GridSize <= 0) return;

        var size = (double) State.InternalGridSize;

        for (var i = 0; i <= State.GridSize; i++)
        {
            var pos = i * State.BoxSize;
            State.GridLines.Add(new GridLine(pos, 0, pos, size));
            State.GridLines.Add(new GridLine(0, pos, size, pos));
        }
    }

    private void OnGridMetricsChanged()
    {
        LoadAnchor();
        RebuildGridLines();
    }

    #endregion
}
