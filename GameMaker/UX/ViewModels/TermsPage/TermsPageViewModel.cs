using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.GameData;
using GameLibrary.Models.Media;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Media;

namespace GameMaker.UX.ViewModels.TermsPage;

/// <summary>
/// The words and icons the game uses for Life, Mana and money, the sound effects the victory and game over screens play, and
/// what a lost battle leads to (Settings\GameSettings.data), so a game can call them HP and MP, Magic, Gold and so on.
/// </summary>
public class TermsPageViewModel(IGameDataService gameDataService, IMediaService mediaService, IAudioService audioService) : BaseViewModel
{
    #region Constants

    private const string None = "(None)";

    #endregion

    #region Properties

    // the settings are replaced when a game loads, so the page reads them fresh each time it opens
    public GameSettings Settings => gameDataService.GameSettings;

    // the files in Media\SFX
    public string[] SfxFiles
    {
        get;
        private set => SetField(ref field, value);
    } = [None];

    public string? SelectedVictorySfx
    {
        get => NameOf(Settings.VictorySfx);
        set
        {
            // the combo box pushes null while its items are swapped, which would wipe the choice
            if (value is null || value == SelectedVictorySfx) return;

            Settings.VictorySfx = Choose(Settings.VictorySfx, value);
            RefreshVictorySfx();
        }
    }

    public bool IsVictorySfxSelected => Settings.VictorySfx is not null;

    public double VictorySfxVolume
    {
        get => Settings.VictorySfx?.Volume ?? AudioDefaults.Volume;
        set
        {
            if (Settings.VictorySfx is null || double.IsNaN(value)) return;

            Settings.VictorySfx.Volume = value;
            OnPropertyChanged();
        }
    }

    public double VictorySfxPitch
    {
        get => Settings.VictorySfx?.Pitch ?? AudioDefaults.Pitch;
        set
        {
            if (Settings.VictorySfx is null || double.IsNaN(value)) return;

            Settings.VictorySfx.Pitch = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedDefeatSfx
    {
        get => NameOf(Settings.DefeatSfx);
        set
        {
            if (value is null || value == SelectedDefeatSfx) return;

            Settings.DefeatSfx = Choose(Settings.DefeatSfx, value);
            RefreshDefeatSfx();
        }
    }

    public bool IsDefeatSfxSelected => Settings.DefeatSfx is not null;

    public double DefeatSfxVolume
    {
        get => Settings.DefeatSfx?.Volume ?? AudioDefaults.Volume;
        set
        {
            if (Settings.DefeatSfx is null || double.IsNaN(value)) return;

            Settings.DefeatSfx.Volume = value;
            OnPropertyChanged();
        }
    }

    public double DefeatSfxPitch
    {
        get => Settings.DefeatSfx?.Pitch ?? AudioDefaults.Pitch;
        set
        {
            if (Settings.DefeatSfx is null || double.IsNaN(value)) return;

            Settings.DefeatSfx.Pitch = value;
            OnPropertyChanged();
        }
    }

    // what losing a battle leads to, an enum the page lists itself
    public DefeatOutcome Defeat
    {
        get => Settings.Defeat;
        set
        {
            if (Settings.Defeat == value) return;

            Settings.Defeat = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Commands

    public RelayCommand PlayVictorySfxCommand => field ??= new RelayCommand(() => Play(Settings.VictorySfx), () => IsVictorySfxSelected);

    public RelayCommand PlayDefeatSfxCommand => field ??= new RelayCommand(() => Play(Settings.DefeatSfx), () => IsDefeatSfxSelected);

    // one stop does for both
    public RelayCommand StopSfxCommand => field ??= new RelayCommand(audioService.StopSfx);

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        SfxFiles = [None, .. mediaService.GetMediaFiles(Cache.Sfx)];
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(Defeat));
        RefreshVictorySfx();
        RefreshDefeatSfx();
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private static string NameOf(AudioFile? file) => file?.Name is { Length: > 0 } name ? name : None;

    // the file picked in the list: none clears the choice, otherwise the choice is made (keeping its volume and pitch)
    private static AudioFile? Choose(AudioFile? current, string value)
    {
        if (value == None) return null;

        current ??= new AudioFile { Path = "SFX" };
        current.Name = value;
        return current;
    }

    private void Play(AudioFile? file)
    {
        if (file is not { Name: { Length: > 0 } name }) return;

        audioService.PlaySfx(name, file.Volume, file.Pitch);
    }

    private void RefreshVictorySfx()
    {
        OnPropertyChanged(nameof(SelectedVictorySfx));
        OnPropertyChanged(nameof(IsVictorySfxSelected));
        OnPropertyChanged(nameof(VictorySfxVolume));
        OnPropertyChanged(nameof(VictorySfxPitch));
        PlayVictorySfxCommand.RaiseCanExecuteChanged();
    }

    private void RefreshDefeatSfx()
    {
        OnPropertyChanged(nameof(SelectedDefeatSfx));
        OnPropertyChanged(nameof(IsDefeatSfxSelected));
        OnPropertyChanged(nameof(DefeatSfxVolume));
        OnPropertyChanged(nameof(DefeatSfxPitch));
        PlayDefeatSfxCommand.RaiseCanExecuteChanged();
    }

    #endregion
}
