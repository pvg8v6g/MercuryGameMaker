using GameLibrary.Services.Graphics;
using GameLibrary.Utilities;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.AppMain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace GameMaker.UX.Components.BalloonIconView;

/// <summary>
/// Picks a balloon's row on the balloon sheet. The picker lists the first frame of every row, and the preview plays
/// the picked row left to right with the balloon's frame time, holds the last frame, then starts over.
/// </summary>
public partial class BalloonIconView
{
    #region Dependency Properties

    public static readonly DependencyProperty RowProperty = DependencyProperty.Register(
        nameof(Row), typeof(int), typeof(BalloonIconView), new PropertyMetadata(0, OnPlaybackChanged));

    public static readonly DependencyProperty FrameTimeProperty = DependencyProperty.Register(
        nameof(FrameTime), typeof(int), typeof(BalloonIconView), new PropertyMetadata(100, OnPlaybackChanged));

    public static readonly DependencyProperty HoldTimeProperty = DependencyProperty.Register(
        nameof(HoldTime), typeof(int), typeof(BalloonIconView), new PropertyMetadata(400, OnPlaybackChanged));

    public int Row
    {
        get => (int) GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public int FrameTime
    {
        get => (int) GetValue(FrameTimeProperty);
        set => SetValue(FrameTimeProperty, value);
    }

    public int HoldTime
    {
        get => (int) GetValue(HoldTimeProperty);
        set => SetValue(HoldTimeProperty, value);
    }

    private static void OnPlaybackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BalloonIconView view) view.Restart().Forget();
    }

    #endregion

    #region Fields

    private readonly DispatcherTimer _timer = new();
    private int _columns;
    private int _frame;

    #endregion

    #region Properties

    // the frame of the picked row being shown
    public CroppedImage Frame { get; } = new();

    private static IGraphicsService GraphicsService => App.Services!.GetRequiredService<IGraphicsService>();

    #endregion

    public BalloonIconView()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => NextFrame().Forget();
        Loaded += (_, _) => Restart().Forget();
        Unloaded += (_, _) => _timer.Stop();
    }

    #region Playback

    private async Task Restart()
    {
        _timer.Stop();
        (_columns, _) = await GraphicsService.GetBalloonDivisions();
        _frame = 0;
        await ShowFrame();
        if (_columns > 0 && IsLoaded) _timer.Start();
    }

    private async Task NextFrame()
    {
        _frame = _frame >= _columns - 1 ? 0 : _frame + 1;
        await ShowFrame();
    }

    // the last frame stays up for the hold time, the others for the frame time
    private async Task ShowFrame()
    {
        var image = await GraphicsService.GetBalloonFrame(Row, _frame);
        Frame.ImageSource = image.ImageSource;
        Frame.Rect = image.Rect;
        var last = _frame >= _columns - 1;
        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(16, last ? HoldTime : FrameTime));
    }

    #endregion

    #region Picker

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        LoadRows().Forget();
        FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
    }

    private async Task LoadRows()
    {
        var (_, rows) = await GraphicsService.GetBalloonDivisions();
        var firstFrames = new List<CroppedImage>();
        for (var row = 0; row < rows; row++) firstFrames.Add(await GraphicsService.GetBalloonFrame(row, 0));

        RowsList.ItemsSource = firstFrames;
        RowsList.SelectedIndex = Row < rows ? Row : -1;
        if (RowsList.SelectedItem is { } selected) RowsList.ScrollIntoView(selected);
    }

    private void RowsList_OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (RowsList.ItemsSource is not List<CroppedImage> rows) return;
        Row = rows.IndexOf((CroppedImage) e.ClickedItem);
        RowsFlyout.Hide();
    }

    #endregion
}
