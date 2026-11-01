using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameMaps;
using MercuryLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// One of the map's full images. The image is found by map id, the only settings are how it is drawn
/// and they are only kept in the map's layer options while they differ from the defaults.
/// </summary>
public partial class MapLayerModel(GameMap map, MapLayerType type, string graphicsDirectory) : PropertyChangedUpdater
{
    #region Constants

    private const string None = "(None)";

    #endregion

    #region Fields

    private readonly MapLayer _layer = FindOption(map, type);

    #endregion

    #region Properties

    public MapLayer Layer => _layer;

    public string Name => _layer.Type.ToString();

    public string ImagePath { get; } = new MapLayer(type).ImagePath(graphicsDirectory, map.Id);

    public bool HasImage => File.Exists(ImagePath);

    public string ImageText => HasImage ? Path.GetFileName(ImagePath) : $"No {Path.GetFileName(ImagePath)} found";

    public string ToolTip => $"Loaded from Graphics\\Maps\\{_layer.Type}\\{Path.GetFileName(ImagePath)}";

    public string[] BlendOptions { get; } = [None, .. Enum.GetNames<BlendMode>()];

    public double Z
    {
        get => _layer.Z;
        set
        {
            if (double.IsNaN(value) || _layer.Z == value) return;
            _layer.Z = value;
            OnSettingsChanged();
        }
    }

    public double Opacity
    {
        get => _layer.Opacity;
        set
        {
            if (double.IsNaN(value) || _layer.Opacity == value) return;
            _layer.Opacity = value;
            OnSettingsChanged();
        }
    }

    public string Blend
    {
        get => _layer.MapBlendMode?.ToString() ?? None;
        set
        {
            // the combo box pushes null while it loads
            if (value is null || value == Blend) return;
            _layer.MapBlendMode = Enum.TryParse<BlendMode>(value, out var blend) ? blend : null;
            OnSettingsChanged();
        }
    }

    public bool IsDefault
    {
        get
        {
            var defaults = MapLayer.Defaults(type);
            return _layer.Z == defaults.Z && _layer.Opacity == defaults.Opacity && _layer.MapBlendMode == defaults.MapBlendMode;
        }
    }

    #endregion

    #region Commands

    public RelayCommand ResetCommand => field ??= new RelayCommand(Reset, () => !IsDefault);

    #endregion

    #region Methods

    private void Reset()
    {
        _layer.Reset();
        OnSettingsChanged();
    }

    private void OnSettingsChanged()
    {
        // only a layer that differs from its defaults is kept as an option on the map
        if (IsDefault) map.LayerOptions.Remove(_layer);
        else if (!map.LayerOptions.Contains(_layer)) map.LayerOptions.Add(_layer);

        OnPropertyChanged(nameof(Z));
        OnPropertyChanged(nameof(Opacity));
        OnPropertyChanged(nameof(Blend));
        OnPropertyChanged(nameof(IsDefault));
        ResetCommand.RaiseCanExecuteChanged();
    }

    // the map's option for this layer, or a new one at the defaults
    private static MapLayer FindOption(GameMap map, MapLayerType type)
    {
        if (map.LayerOptions.FirstOrDefault(x => x.Type == type) is { } option) return option;

        var layer = new MapLayer(type);
        layer.Reset();
        return layer;
    }

    #endregion
}
