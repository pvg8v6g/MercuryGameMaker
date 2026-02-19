using System.Collections.ObjectModel;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Utilities.ComponentModels;
using Attribute = GameLibrary.Models.Attributes.Attribute;

namespace GameMaker.UX.ViewModels.AttributesPage;

public class AttributesPageViewModel(IGameDataService gameDataService, IJsonService jsonService, IGraphicsService graphicsService)
    : BaseViewModel<Attribute>(jsonService)
{
    #region Properties

    public CroppedImage AttributeIcon
    {
        get;
        set => SetField(ref field, value);
    } = new();

    public IGameDataService GameDataService => gameDataService;

    protected override ObservableCollection<Attribute> EntityCollection => gameDataService.Attributes;

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        SelectedIndex = 0;
        if (EntityCollection.Count < 1) return;
        AttributeIcon = await graphicsService.GetIcon(EntityCollection[SelectedIndex].Icon);
    }

    #endregion
}
