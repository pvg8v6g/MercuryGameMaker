using System.Collections.ObjectModel;
using GameLibrary.Models.Animations;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.ComponentModels;
using GameLibrary.Commands;
using Windows.Foundation;

namespace GameMaker.UX.ViewModels.AnimationsPage;

public class AnimationsPageViewModel(IGameDataService gameDataService, IGraphicsService graphicsService, IJsonService jsonService)
    : BaseViewModel<Animation>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Animation> EntityCollection => gameDataService.Animations;

    public FileSelection[] AnimationImageFiles
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public int SelectedAnimationImageFilesIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public int SelectedFrame
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public int AnchorX
    {
        get;
        set => SetField(ref field, value);
    }

    public int AnchorY
    {
        get;
        set => SetField(ref field, value);
    }

    public int AnchorSize
    {
        get;
        set => SetField(ref field, value);
    }

    public double CanvasWidth
    {
        get;
        set => SetField(ref field, value);
    }

    public double CanvasHeight
    {
        get;
        set => SetField(ref field, value);
    }

    public RelayCommand<Size> CanvasSizeChangedCommand => new(size =>
    {
        CanvasWidth = size.Width;
        CanvasHeight = size.Height;
        UpdateAnchor();
    });

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        RefreshAnimationImageFiles();
        AnchorSize = gameDataService.GridSize;
        return Task.CompletedTask;
    }

    protected override Task OnSelectedIndexChanged(int index)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Private methods

    private void RefreshAnimationImageFiles()
    {
        AnimationImageFiles = graphicsService.GetAnimationImages();
    }

    private void UpdateAnchor()
    {
        AnchorX = (int) ((CanvasWidth - AnchorSize) / 2.0);
        AnchorY = (int) ((CanvasHeight - AnchorSize) / 2.0);
    }

    #endregion
}
