using System.Collections.ObjectModel;
using System.Drawing;
using GameLibrary.Behaviors;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Utilities.Calculations;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.UX.ViewModels;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.GridArea;

public class AreaCreatorViewModel : BaseViewModel
{
    #region Properties

    public ObservableCollection<Area> Hitboxes
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public ObservableCollection<GridLine> GridLines { get; } = [];

    public CroppedImage? CharacterImage
    {
        get;
        set => SetField(ref field, value);
    }

    private Point? StartPosition
    {
        get;
        set => SetField(ref field, value);
    }

    public Position? AnchorPosition
    {
        get;
        set => SetField(ref field, value);
    }

    public int GridSize
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(InternalGridSize));
            LoadAnchor();
            RebuildGridLines();
        }
    } = 21;

    public int BoxSize
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(InternalGridSize));
            LoadAnchor();
            RebuildGridLines();
        }
    } = 48;

    public int InternalGridSize => GridSize * BoxSize;

    public bool ShowGrid
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool ShowPreview => StartPosition is not null;

    public Position? PreviewPosition
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Commands

    public RelayCommand ClearAllCommand => new(ClearAllAction);

    public RelayCommand<PointerEventInfo> PointerPressedCommand => new(PointerPressedAction);

    public RelayCommand<PointerEventInfo> HitboxPointerPressedCommand => new(HitboxPointerPressedAction);

    public RelayCommand<PointerEventInfo> PointerMovedCommand => new(PointerMovedAction);

    public RelayCommand<PointerEventInfo> PointerReleasedCommand => new(PointerReleasedAction);

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        LoadAnchor();
        RebuildGridLines();
        await Task.CompletedTask;
    }

    private void HitboxPointerPressedAction(PointerEventInfo e)
    {
        if (!e.Properties.IsRightButtonPressed) return;

        if (e.Sender is FrameworkElement { DataContext: Area area })
        {
            Hitboxes.Remove(area);
            e.Args.Handled = true;
        }
    }

    private void ClearAllAction()
    {
        Hitboxes.Clear();
    }

    private void PointerPressedAction(PointerEventInfo e)
    {
        if (!e.Properties.IsLeftButtonPressed) return;
        var pos = e.Position;
        var x = Calculations.RoundToMultiple(pos.X, BoxSize, Rounding.Down);
        var y = Calculations.RoundToMultiple(pos.Y, BoxSize, Rounding.Down);
        StartPosition = new Point(x, y);
        PreviewPosition = new Position { X = StartPosition.Value.X, Y = StartPosition.Value.Y, Width = BoxSize, Height = BoxSize };
        OnPropertyChanged(nameof(ShowPreview));
    }

    private void PointerMovedAction(PointerEventInfo e)
    {
        if (StartPosition is null || !e.Properties.IsLeftButtonPressed || PreviewPosition is null) return;
        var pos = e.Position;
        if (pos.X < 0) pos.X = 0;
        if (pos.Y < 0) pos.Y = 0;
        if (pos.X > InternalGridSize) pos.X = InternalGridSize;
        if (pos.Y > InternalGridSize) pos.Y = InternalGridSize;

        var roundDirectionX = pos.X > StartPosition.Value.X ? Rounding.Up : Rounding.Down;
        var roundDirectionY = pos.Y > StartPosition.Value.Y ? Rounding.Up : Rounding.Down;

        var x = Math.Min(Calculations.RoundToMultiple(pos.X, BoxSize, roundDirectionX), StartPosition.Value.X);
        var y = Math.Min(Calculations.RoundToMultiple(pos.Y, BoxSize, roundDirectionY), StartPosition.Value.Y);

        var extraSizingW = pos.X >= StartPosition.Value.X ? 0 : BoxSize;
        var extraSizingH = pos.Y >= StartPosition.Value.Y ? 0 : BoxSize;

        var w = Math.Max(BoxSize, Math.Max(Calculations.RoundToMultiple(pos.X, BoxSize, roundDirectionX), StartPosition.Value.X) - x) + extraSizingW;
        var h = Math.Max(BoxSize, Math.Max(Calculations.RoundToMultiple(pos.Y, BoxSize, roundDirectionY), StartPosition.Value.Y) - y) + extraSizingH;
        PreviewPosition.X = x;
        PreviewPosition.Y = y;
        PreviewPosition.Width = w;
        PreviewPosition.Height = h;
    }

    private void PointerReleasedAction(PointerEventInfo e)
    {
        if (!e.Properties.IsLeftButtonPressed || PreviewPosition is null) return;
        StartPosition = null;
        OnPropertyChanged(nameof(ShowPreview));
        var x = AnchorPosition is null ? PreviewPosition.X : PreviewPosition.X - AnchorPosition.X;
        var y = AnchorPosition is null ? PreviewPosition.Y : PreviewPosition.Y - AnchorPosition.Y;
        var area = new Area { X = x, Y = y, Width = PreviewPosition.Width, Height = PreviewPosition.Height };
        Hitboxes.Add(area);
    }

    #endregion

    #region Listeners

    public void OnHitboxesChanged(ObservableCollection<Area> hitboxes)
    {
        Hitboxes = hitboxes;
    }

    public void OnCharacterImageChanged(CroppedImage? croppedImage)
    {
        CharacterImage = croppedImage;
    }

    #endregion

    #region Private Methods

    private void LoadAnchor()
    {
        if (CharacterImage?.Rect is null) return;
        var w = (InternalGridSize / 2.0) - (CharacterImage.Rect.Value.Width / 2.0);
        var h = (InternalGridSize / 2.0) - (CharacterImage.Rect.Value.Height / 2.0);
        var rw = Calculations.RoundToMultiple(w, BoxSize, Rounding.Down);
        var rh = Calculations.RoundToMultiple(h, BoxSize, Rounding.Down);
        AnchorPosition = new Position { X = rw, Y = rh };
    }

    private void RebuildGridLines()
    {
        GridLines.Clear();

        if (BoxSize <= 0 || GridSize <= 0) return;

        var size = (double) InternalGridSize;

        for (var i = 0; i <= GridSize; i++)
        {
            var pos = i * BoxSize;
            GridLines.Add(new GridLine(pos, 0, pos, size));
            GridLines.Add(new GridLine(0, pos, size, pos));
        }
    }

    #endregion
}

// private final EventHandler<MouseEvent> mouseUpListener = mouseEvent -> {
//     if (mouseEvent.getButton() == MouseButton.SECONDARY || _rectangle == null) return;
//     var x = _anchorPosition == null ? _rectangle.getTranslateX() : _rectangle.getTranslateX() - _anchorPosition.x;
//     var y = _anchorPosition == null ? _rectangle.getTranslateY() : _rectangle.getTranslateY() - _anchorPosition.y;
//     var area = new Area(x, y, _rectangle.getWidth(), _rectangle.getHeight());
//     _rectangle.area = area;
//     variable.areas.add(area);
//     _rectangle = null;
//     _startPoint = null;
// };
