using System.Collections.ObjectModel;
using GameLibrary.Models.Balloons;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;

namespace GameMaker.UX.ViewModels.BalloonIconsPage;

/// <summary>
/// The Balloon Icons settings: each balloon is a named row of the balloon sheet and how fast it plays.
/// Patrols, page commands and movement routes pick balloons from this list by name.
/// </summary>
public partial class BalloonIconsPageViewModel(IGameDataService gameDataService, IGraphicsService graphicsService, IJsonService jsonService)
    : BaseViewModel<BalloonIcon>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<BalloonIcon> EntityCollection => gameDataService.BalloonIcons;

    // where the sheet goes, shown until it is there
    public string SheetPath => graphicsService.GetBalloonsPath();

    public bool SheetMissing
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Actions

    protected override Task LoadedAction()
    {
        SheetMissing = !File.Exists(SheetPath);
        return Task.CompletedTask;
    }

    #endregion
}
