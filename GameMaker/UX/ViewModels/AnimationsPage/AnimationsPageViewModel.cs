using System.Collections.ObjectModel;
using GameLibrary.Models.Animations;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.ComponentModels;

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

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        RefreshAnimationImageFiles();
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

    // private void populateAnimationFileList() {
    //     if (getSelectedEntity() == null || animationFiles.any()) return;
    //     var files = FileUtility.getFilesInFolder("Graphics/Animations/").select(x -> {
    //         var model = new FileSelectionModel();
    //         model.name = x.getName();
    //         model.path = x.getAbsolutePath();
    //         return model;
    //     })
    //     .orderBy(x -> x.name)
    //         .toList();
    //     animationFiles.setAll(files);
    // }

    #endregion
}
