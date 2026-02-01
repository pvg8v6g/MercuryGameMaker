using GameLibrary.Commands;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameMaker.UX.ViewModels.TopBar;

public class TopBarViewModel : BaseViewModel
{
    #region Properties

    public BitmapSource? MainImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? ActorsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? DisciplinesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? ArtesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? ItemsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? EquipmentImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? EnemiesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? TroopsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? StatesImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? AnimationsImage
    {
        get;
        set => SetField(ref field, value);
    }

    public BitmapSource? SettingsImage
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Actions

    protected override void LoadedAction()
    {
    }

    private RelayCommand<string>? _topBarCommand;
    public RelayCommand<string> TopBarCommand => _topBarCommand ??= new(TopBarAction);

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
