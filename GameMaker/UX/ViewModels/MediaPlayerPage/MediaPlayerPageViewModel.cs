using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Models.Media;
using GameLibrary.Services.Media;

namespace GameMaker.UX.ViewModels.MediaPlayerPage;

public class MediaPlayerPageViewModel(IMediaService mediaService, IAudioService audioService) : BaseViewModel
{
    #region Properties

    public ObservableCollection<MediaSelectionModel> TreeRoot
    {
        get;
        private set => SetField(ref field, value);
    } = [];

    public MediaSelectionModel? SelectedFile
    {
        get;
        set
        {
            SetField(ref field, value);
            ControlsEnabled = value?.Path is not null;
        }
    }

    public double Volume
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public double Pitch
    {
        get;
        set => SetField(ref field, value);
    } = 1.0;

    public bool ControlsEnabled
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Commands

    public RelayCommand PlayCommand => new(PlaySound);

    public RelayCommand StopCommand => new(StopSound);

    public RelayCommand<MediaSelectionModel> PlaySelectedCommand => new(PlaySelected);

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        LoadFiles();
        return Task.CompletedTask;
    }

    private void LoadFiles()
    {
        var root = new ObservableCollection<MediaSelectionModel>
        {
            new() { Name = "- None -", Path = null }
        };

        var categories = new[]
        {
            ("BGM", mediaService.GetBgmPath()),
            ("BGS", mediaService.GetBgsPath()),
            ("ME", mediaService.GetMePath()),
            ("SFX", mediaService.GetSfxPath()),
            ("Footsteps", mediaService.GetFootstepsPath())
        };

        foreach (var (name, path) in categories)
        {
            var category = new MediaSelectionModel { Name = name, Path = null };

            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    category.Children.Add(new MediaSelectionModel
                    {
                        Name = Path.GetFileName(file),
                        Path = name
                    });
                }
            }

            root.Add(category);
        }

        TreeRoot = root;
    }

    private void PlaySelected(MediaSelectionModel model)
    {
        SelectedFile = model;
        PlaySound();
    }

    private void PlaySound()
    {
        if (SelectedFile?.Name is null || SelectedFile.Path is null) return;

        switch (SelectedFile.Path)
        {
            case "BGM":
                audioService.PlayBgm(SelectedFile.Name, Volume, Pitch);
                break;
            case "BGS":
                audioService.PlayBgs(SelectedFile.Name, Volume, Pitch);
                break;
            case "ME":
                audioService.PlayMe(SelectedFile.Name, Volume, Pitch);
                break;
            case "SFX":
                audioService.PlaySfx(SelectedFile.Name, Volume, Pitch);
                break;
            case "Footsteps":
                audioService.PlayFootstep(SelectedFile.Name, Volume, Pitch);
                break;
        }
    }

    private void StopSound()
    {
        audioService.StopBgm();
        audioService.StopBgs();
        audioService.StopMe();
    }

    #endregion
}
