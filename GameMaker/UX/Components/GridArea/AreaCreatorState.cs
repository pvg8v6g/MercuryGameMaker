using System.Collections.ObjectModel;
using System.Drawing;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Components.GridArea;

public sealed partial class AreaCreatorState(Action? gridMetricsChanged = null) : PropertyChangedUpdater
{
    private readonly Action? _gridMetricsChanged = gridMetricsChanged;

    public ObservableCollection<GridLine> GridLines { get; } = [];

    public Position? AnchorPosition
    {
        get;
        set => SetField(ref field, value);
    }

    // where the character picture's top left goes: on the anchor, or standing on it (feet on the anchor tile)
    public Position? SpritePosition
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
            _gridMetricsChanged?.Invoke();
        }
    } = 21;

    public int BoxSize
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(InternalGridSize));
            OnPropertyChanged(nameof(AnchorBoxSize));
            OnPropertyChanged(nameof(AnchorHalfBoxSize));
            _gridMetricsChanged?.Invoke();
        }
    } = 48;

    public int InternalGridSize => GridSize * BoxSize;

    // the anchor's own size when it's a real tile whatever the grid is divided into (a character standing on it),
    // 0 to draw it one grid box big
    public int AnchorSize
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(AnchorBoxSize));
            OnPropertyChanged(nameof(AnchorHalfBoxSize));
        }
    }

    public int AnchorBoxSize => AnchorSize > 0 ? AnchorSize : BoxSize;

    public double AnchorHalfBoxSize => AnchorBoxSize / 2.0;

    public bool ShowGrid
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public Point? StartPosition
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(ShowPreview));
        }
    }

    public bool ShowPreview => StartPosition is not null;

    public Position? PreviewPosition
    {
        get;
        set => SetField(ref field, value);
    }
}
