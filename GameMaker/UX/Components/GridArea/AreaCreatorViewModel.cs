using System.Collections.ObjectModel;
using System.Drawing;
using GameLibrary.Behaviors;
using GameLibrary.Commands;
using GameLibrary.Models.Areas;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.UX.ViewModels;

namespace GameMaker.UX.Components.GridArea;

public class AreaCreatorViewModel : BaseViewModel
{
    #region Properties

    public ObservableCollection<Hitbox> Hitboxes
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

    public int GridSize
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(InternalGridSize));
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
            RebuildGridLines();
        }
    } = 48;

    public int InternalGridSize => GridSize * BoxSize;

    public bool ShowGrid
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool ShowPreview
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public double PreviewX
    {
        get;
        set => SetField(ref field, value);
    }

    public double PreviewY
    {
        get;
        set => SetField(ref field, value);
    }

    public double PreviewWidth
    {
        get;
        set => SetField(ref field, value);
    }

    public double PreviewHeight
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Commands

    public RelayCommand ClearAllCommand => new(ClearAllAction);

    public RelayCommand<PointerEventInfo> PointerPressedCommand => new(PointerPressedAction);

    public RelayCommand<PointerEventInfo> PointerMovedCommand => new(PointerMovedAction);

    public RelayCommand<PointerEventInfo> PointerReleasedCommand => new(PointerReleasedAction);

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        RebuildGridLines();
        await Task.CompletedTask;
    }

    #endregion

    #region Listeners

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

    public void OnHitboxesChanged(ObservableCollection<Hitbox> hitboxes)
    {
        Hitboxes = hitboxes;
    }

    public void OnCharacterImageChanged(CroppedImage? croppedImage)
    {
        CharacterImage = croppedImage;
    }

    private void ClearAllAction()
    {
        Hitboxes.Clear();
    }

    private void PointerPressedAction(PointerEventInfo e)
    {
        var pos = e.Position;
        StartPosition = new Point((int) pos.X, (int) pos.Y);
        PreviewX = pos.X;
        PreviewY = pos.Y;
        PreviewWidth = BoxSize;
        PreviewHeight = BoxSize;
        ShowPreview = true;
        Console.WriteLine("pressed");
    }

    private void PointerMovedAction(PointerEventInfo e)
    {
        if (StartPosition is null) return;
        var pos = e.Position;
        Console.WriteLine("moved");
    }

    private void PointerReleasedAction(PointerEventInfo e)
    {
        StartPosition = null;
        ShowPreview = false;
        Console.WriteLine("released");
    }

    #endregion
}
