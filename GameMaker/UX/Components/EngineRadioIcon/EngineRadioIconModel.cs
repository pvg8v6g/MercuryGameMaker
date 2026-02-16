using System.Collections.ObjectModel;
using GameLibrary.Models;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Components.EngineRadioIcon;

public class EngineRadioIconModel : PropertyChangedUpdater
{
    public CroppedImage? CroppedImage
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsChecked
    {
        get;
        set => SetField(ref field, value);
    }

    public string CommandIndex
    {
        get;
        set => SetField(ref field, value);
    } = "0";

    public string Tooltip
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public ObservableCollection<MenuItemModel> MenuItems
    {
        get;
        set => SetField(ref field, value);
    } = [];
}

public class MenuItemModel : PropertyChangedUpdater
{
    public string Header
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;

    public string CommandIndex
    {
        get;
        set => SetField(ref field, value);
    } = string.Empty;
}
