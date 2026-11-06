using GameLibrary.Commands;
using GameLibrary.Models.GameMaps;
using MercuryLibrary.Enumerations;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// Edits how one of the map's full images is drawn: its z, opacity and blend.
/// </summary>
public abstract partial class MapImageModel(MapImageSettings settings) : PropertyChangedUpdater
{
    #region Constants

    private const string None = "(None)";

    #endregion

    #region Properties

    protected MapImageSettings Settings => settings;

    public abstract string Name { get; }

    public string[] BlendOptions { get; } = [None, .. Enum.GetNames<BlendMode>()];

    public double Z
    {
        get => settings.Z;
        set
        {
            if (double.IsNaN(value) || settings.Z == value) return;
            settings.Z = value;
            OnSettingsChanged();
        }
    }

    public double Opacity
    {
        get => settings.Opacity;
        set
        {
            if (double.IsNaN(value) || settings.Opacity == value) return;
            settings.Opacity = value;
            OnSettingsChanged();
        }
    }

    public string Blend
    {
        get => settings.MapBlendMode?.ToString() ?? None;
        set
        {
            // the combo box pushes null while it loads
            if (value is null || value == Blend) return;
            settings.MapBlendMode = Enum.TryParse<BlendMode>(value, out var blend) ? blend : null;
            OnSettingsChanged();
        }
    }

    public bool IsDefault
    {
        get
        {
            var defaults = settings.DefaultSettings;
            return settings.Z == defaults.Z && settings.Opacity == defaults.Opacity && settings.MapBlendMode == defaults.MapBlendMode;
        }
    }

    #endregion

    #region Commands

    public RelayCommand ResetCommand => field ??= new RelayCommand(Reset, () => !IsDefault);

    #endregion

    #region Methods

    private void Reset()
    {
        settings.Reset();
        OnSettingsChanged();
    }

    protected virtual void OnSettingsChanged()
    {
        OnPropertyChanged(nameof(Z));
        OnPropertyChanged(nameof(Opacity));
        OnPropertyChanged(nameof(Blend));
        OnPropertyChanged(nameof(IsDefault));
        ResetCommand.RaiseCanExecuteChanged();
    }

    #endregion
}
