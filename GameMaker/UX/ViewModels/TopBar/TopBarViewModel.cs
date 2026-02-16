using System.Collections.ObjectModel;
using GameLibrary.Commands;
using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.Services.Navigation;
using GameMaker.UX.Components.EngineRadioIcon;

namespace GameMaker.UX.ViewModels.TopBar;

public class TopBarViewModel(IGraphicsService graphicsService, INavigationService navigationService) : BaseViewModel
{
    #region Properties

    public ObservableCollection<EngineRadioIconModel> EngineImages
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public EngineRadioIconModel? MediaImage
    {
        get;
        set => SetField(ref field, value);
    }

    public EngineRadioIconModel? SaveImage
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        var engineIconPath = graphicsService.GetEngineIconPath();

        MediaImage = new EngineRadioIconModel
        {
            CroppedImage = new CroppedImage
            {
                ImageSource = engineIconPath,
                Rect = await graphicsService.GetEngineIconViewport(15)
            },
            CommandIndex = "11",
            Tooltip = "Media"
        };

        SaveImage = new EngineRadioIconModel
        {
            CroppedImage = new CroppedImage
            {
                ImageSource = engineIconPath,
                Rect = await graphicsService.GetEngineIconViewport(11)
            },
            CommandIndex = "12",
            Tooltip = "Save"
        };

        EngineRadioIconModel[] buttonIcons =
        [
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = true,
                CommandIndex = "0",
                Tooltip = "Main"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "1",
                Tooltip = "Actors"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "2",
                Tooltip = "Disciplines"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "3",
                Tooltip = "Artes"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "4",
                Tooltip = "Items"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "5",
                Tooltip = "Equipment"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "6",
                Tooltip = "Enemies"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "7",
                Tooltip = "Troops"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "8",
                Tooltip = "States"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "9",
                Tooltip = "Animations"
            },
            new()
            {
                CroppedImage = new CroppedImage
                {
                    ImageSource = engineIconPath,
                },
                IsChecked = false,
                CommandIndex = "10",
                Tooltip = "Settings",
                MenuItems =
                [
                    new MenuItemModel { Header = "Attributes", CommandIndex = "10.1" },
                    new MenuItemModel { Header = "Elements", CommandIndex = "10.2" },
                    new MenuItemModel { Header = "Growths", CommandIndex = "10.3" },
                ]
            }
        ];

        var tasks = buttonIcons
            .Select(async (x, i) =>
            {
                x.CroppedImage?.Rect = await graphicsService.GetEngineIconViewport(i);
                return x;
            })
            .ToArray();

        EngineImages = new ObservableCollection<EngineRadioIconModel>(await Task.WhenAll(tasks));
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
            case "10.1":
                EngineImages.FirstOrDefault(x => x.CommandIndex == "10")?.IsChecked = true;
                navigationService.NavigateTo<Views.AttributesPage.AttributesPage>();
                break;
            case "10.2":
                EngineImages.FirstOrDefault(x => x.CommandIndex == "10")?.IsChecked = true;
                break;
            case "10.3":
                EngineImages.FirstOrDefault(x => x.CommandIndex == "10")?.IsChecked = true;
                break;
            case "11": // media
                break;
            case "12": // saving
                // navigationService.ShowProgressPopup<SaveProjectDataTask>("Saving Project Files");
                break;
        }
    }

    #endregion
}
