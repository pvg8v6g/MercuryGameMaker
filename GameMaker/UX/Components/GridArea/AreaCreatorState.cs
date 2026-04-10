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
            _gridMetricsChanged?.Invoke();
        }
    } = 48;

    public int InternalGridSize => GridSize * BoxSize;

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
