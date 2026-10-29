using System.Collections.ObjectModel;
using System.Windows.Input;
using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Utilities.Calculations;
using GameMaker.UX.Components.GridArea;
using GameMaker.UX.Models.TroopsPage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Components.TroopArena;

public partial class TroopArena
{
    #region Registered Dependencies

    public static readonly DependencyProperty EnemiesProperty = DependencyProperty.Register(
        nameof(Enemies), typeof(ObservableCollection<TroopEnemyModel>), typeof(TroopArena), new PropertyMetadata(null, OnBindingsChanged));

    public ObservableCollection<TroopEnemyModel>? Enemies
    {
        get => (ObservableCollection<TroopEnemyModel>?) GetValue(EnemiesProperty);
        set => SetValue(EnemiesProperty, value);
    }

    public static readonly DependencyProperty BackgroundSourceProperty = DependencyProperty.Register(
        nameof(BackgroundSource), typeof(ImageSource), typeof(TroopArena), new PropertyMetadata(null, OnBindingsChanged));

    public ImageSource? BackgroundSource
    {
        get => (ImageSource?) GetValue(BackgroundSourceProperty);
        set => SetValue(BackgroundSourceProperty, value);
    }

    public static readonly DependencyProperty ArenaSizeProperty = DependencyProperty.Register(
        nameof(ArenaSize), typeof(int), typeof(TroopArena), new PropertyMetadata(1008, OnGridMetricsChanged));

    public int ArenaSize
    {
        get => (int) GetValue(ArenaSizeProperty);
        set => SetValue(ArenaSizeProperty, value);
    }

    public static readonly DependencyProperty BlockSizeProperty = DependencyProperty.Register(
        nameof(BlockSize), typeof(int), typeof(TroopArena), new PropertyMetadata(48, OnGridMetricsChanged));

    public int BlockSize
    {
        get => (int) GetValue(BlockSizeProperty);
        set => SetValue(BlockSizeProperty, value);
    }

    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(TroopArena), new PropertyMetadata(true, OnBindingsChanged));

    public bool ShowGrid
    {
        get => (bool) GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    // executed with the snapped Point the selected enemy's 0,0 should move to
    public static readonly DependencyProperty PlaceCommandProperty = DependencyProperty.Register(
        nameof(PlaceCommand), typeof(ICommand), typeof(TroopArena), new PropertyMetadata(null));

    public ICommand? PlaceCommand
    {
        get => (ICommand?) GetValue(PlaceCommandProperty);
        set => SetValue(PlaceCommandProperty, value);
    }

    // executed with the TroopEnemyModel that was right-clicked in the arena
    public static readonly DependencyProperty SelectCommandProperty = DependencyProperty.Register(
        nameof(SelectCommand), typeof(ICommand), typeof(TroopArena), new PropertyMetadata(null));

    public ICommand? SelectCommand
    {
        get => (ICommand?) GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    #endregion

    #region Properties

    public ObservableCollection<GridLine> GridLines { get; } = [];

    #endregion

    #region Constructor

    public TroopArena()
    {
        InitializeComponent();
        RebuildGrid();
    }

    #endregion

    #region Event Handlers

    private void OnRootGridPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(RootGrid);
        if (!point.Properties.IsLeftButtonPressed) return;

        var snapped = Snap(point.Position);
        if (PlaceCommand?.CanExecute(snapped) == true) PlaceCommand.Execute(snapped);
    }

    private void OnRootGridPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var snapped = Snap(e.GetCurrentPoint(RootGrid).Position);
        Canvas.SetLeft(HoverBlock, snapped.X);
        Canvas.SetTop(HoverBlock, snapped.Y);
        HoverBlock.Visibility = Visibility.Visible;
    }

    private void OnRootGridPointerExited(object sender, PointerRoutedEventArgs e)
    {
        HoverBlock.Visibility = Visibility.Collapsed;
    }

    private void OnEnemyPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint((UIElement) sender).Properties.IsRightButtonPressed) return;
        if (sender is not FrameworkElement { DataContext: TroopEnemyModel enemy }) return;

        if (SelectCommand?.CanExecute(enemy) == true) SelectCommand.Execute(enemy);
        e.Handled = true;
    }

    #endregion

    #region Listeners

    private static void OnBindingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TroopArena control) control.Bindings.Update();
    }

    private static void OnGridMetricsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TroopArena control) return;
        control.RebuildGrid();
        control.Bindings.Update();
    }

    #endregion

    #region Private Methods

    private Point Snap(Point position)
    {
        var block = Math.Max(1, BlockSize);
        var max = Math.Max(0, ArenaSize - block);
        var x = Math.Clamp(Calculations.RoundToMultiple(position.X, block, Rounding.Down), 0, max);
        var y = Math.Clamp(Calculations.RoundToMultiple(position.Y, block, Rounding.Down), 0, max);
        return new Point(x, y);
    }

    private void RebuildGrid()
    {
        RootGrid.Clip = new RectangleGeometry { Rect = new Rect(0, 0, ArenaSize, ArenaSize) };

        GridLines.Clear();
        if (BlockSize <= 0 || ArenaSize <= 0) return;

        for (var pos = 0; pos <= ArenaSize; pos += BlockSize)
        {
            GridLines.Add(new GridLine(pos, 0, pos, ArenaSize));
            GridLines.Add(new GridLine(0, pos, ArenaSize, pos));
        }
    }

    #endregion
}
