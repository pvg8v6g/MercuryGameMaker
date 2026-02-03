using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Models;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Location;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameMaker.UX.ViewModels.TopBar;

public class TopBarViewModel(IGraphicsService graphicsService, ILocationService locationService) : BaseViewModel
{
    #region Properties

    public Canvas? TestImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? MainImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? ActorsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? DisciplinesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? ArtesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? ItemsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? EquipmentImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? EnemiesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? TroopsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? StatesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? AnimationsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapImage? SettingsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<EngineImage> EngineImages
    {
        get;
        set => SetField(ref field, value);
    } = [];

    #endregion

    #region Actions

    protected override async Task LoadedAction()
    {
        EngineImages =
        [
            new EngineImage
            {
                ImageSource = Path.Combine(locationService.GameMakerGraphicsDirectory!, "Icons.png"),
                Rect = new Rect(0, 0, 48, 48)
            }
        ];
        // TestImage = await graphicsService.GetEngineIconImproved(1);
        // var filePath = Path.Combine(AppContext.BaseDirectory, "Graphics", "Icons.png");
        // var uri = new Uri(filePath, UriKind.Absolute);
        // var bi = await graphicsService.GetEngineIcon(0);
        // var bi = new BitmapImage(uri);
        //
        // bi.ImageOpened += (s, e) => { Console.WriteLine("opened"); };

        // var tcs = new TaskCompletionSource<bool>();
        // bi.ImageOpened += (s, e) => tcs.SetResult(true);
        // bi.ImageFailed += (s, e) => tcs.SetResult(false);
        // await tcs.Task;

        // TestUri = bi;
    }

    public RelayCommand<string> TopBarCommand => new(TopBarAction);

    private void TopBarAction(string index)
    {
        switch (index)
        {
            case "0":
                // navigationService.NavigateTo<InfantryPageViewModel>();
                break;
            case "1":
                break;
            case "2":
                // navigationService.NavigateTo<ArmorPageViewModel>();
                break;
            case "3":
                // navigationService.NavigateTo<LocomotorPageViewModel>();
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
