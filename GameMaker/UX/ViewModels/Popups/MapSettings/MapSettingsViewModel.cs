using GameMaker.UX.ViewModels.MainPage;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.MapSettings;

/// <summary>
/// The selected map's settings, shown in full when a map is created and when it is edited.
/// </summary>
public class MapSettingsViewModel(MainPageViewModel editor, bool isNew) : PropertyChangedUpdater
{
    #region Properties

    public MainPageViewModel Editor => editor;

    public string Title => (isNew, editor.IsFolderSelected) switch
    {
        (true, true) => "New Folder",
        (true, false) => "New Map",
        (false, true) => "Folder Settings",
        _ => "Map Settings"
    };

    public string PrimaryText => isNew ? "Create" : "Done";

    // a new map can be thrown away, edits to an existing map are made as you go
    public string? CancelText => isNew ? "Cancel" : null;

    #endregion
}
