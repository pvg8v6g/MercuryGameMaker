using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Models;
using GameLibrary.Services.Graphics;
using GameMaker.Services.Navigation;

namespace GameMaker.UX.ViewModels.TopBar;

public class TopBarViewModel(IGraphicsService graphicsService, INavigationService navigationService) : BaseViewModel
{
    #region Properties

    public ObservableCollection<TopBarModel> EngineImages
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public TopBarModel? SaveImage
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        var engineIconPath = graphicsService.GetEngineIconPath();
        SaveImage = new TopBarModel
        {
            CroppedImage = new CroppedImage
            {
                ImageSource = engineIconPath,
                Rect = await graphicsService.GetEngineIconViewport(11)
            },
            CommandIndex = "11",
            Tooltip = "Save"
        };
        EngineImages =
        [
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(0)
                },
                IsChecked = true,
                CommandIndex = "0",
                Tooltip = "Main"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(1)
                },
                IsChecked = false,
                CommandIndex = "1",
                Tooltip = "Actors"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(2)
                },
                IsChecked = false,
                CommandIndex = "2",
                Tooltip = "Disciplines"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(3)
                },
                IsChecked = false,
                CommandIndex = "3",
                Tooltip = "Artes"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(4)
                },
                IsChecked = false,
                CommandIndex = "4",
                Tooltip = "Items"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(5)
                },
                IsChecked = false,
                CommandIndex = "5",
                Tooltip = "Equipment"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(6)
                },
                IsChecked = false,
                CommandIndex = "6",
                Tooltip = "Enemies"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(7)
                },
                IsChecked = false,
                CommandIndex = "7",
                Tooltip = "Troops"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(8)
                },
                IsChecked = false,
                CommandIndex = "8",
                Tooltip = "States"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(9)
                },
                IsChecked = false,
                CommandIndex = "9",
                Tooltip = "Animations"
            },
            new TopBarModel
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                    Rect = await graphicsService.GetEngineIconViewport(10)
                },
                IsChecked = false,
                CommandIndex = "10",
                Tooltip = "Settings"
            }
        ];
    }

    public RelayCommand<string> TopBarCommand => new(TopBarAction);

    private void TopBarAction(string index)
    {
        switch (index)
        {
            case "0":
                break;
            case "1":
                navigationService.NavigateTo<Views.ActorsPage.ActorsPage>();
                break;
            case "2":
                break;
            case "3":
                break;
            case "4":
                break;
            case "5":
                break;
            case "6":
                break;
            case "7":
                break;
            case "8":
                break;
            case "9":
                break;
            case "10":
                break;
            case "11": // saving
                // navigationService.ShowProgressPopup<SaveProjectDataTask>("Saving Project Files");
                break;
        }
    }

    #endregion
}
