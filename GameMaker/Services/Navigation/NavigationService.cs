using GameMaker.UX.ViewModels.Popups.FillProps;
using GameMaker.UX.Views.Popups.FillProps;
using GameLibrary.Tasks;
using GameMaker.UX.Components.Popup;
using GameMaker.UX.ViewModels.Popups.ActionEditor;
using GameMaker.UX.ViewModels.Popups.BranchActionEditor;
using GameMaker.UX.ViewModels.Popups.AnimationEffects;
using GameMaker.UX.ViewModels.Popups.ConditionEditor;
using GameMaker.UX.ViewModels.Popups.EnemyArteEditor;
using GameMaker.UX.ViewModels.MainPage;
using GameMaker.UX.ViewModels.Popups.EntityEditor;
using GameMaker.UX.ViewModels.Popups.GameStart;
using GameMaker.UX.ViewModels.Popups.MapIdConflict;
using GameMaker.UX.ViewModels.Popups.MapSettings;
using GameMaker.UX.ViewModels.Popups.FrameSpanning;
using GameMaker.UX.ViewModels.Popups.MovementEditor;
using GameMaker.UX.ViewModels.Popups.MovementRouteEditor;
using GameMaker.UX.Views.MainWindow;
using GameMaker.UX.Views.Popups.ActionEditor;
using GameMaker.UX.Views.Popups.AreaEditor;
using GameMaker.UX.Views.Popups.PathEditor;
using GameMaker.UX.Views.Popups.PropEditor;
using GameMaker.UX.Views.Popups.BranchActionEditor;
using GameMaker.UX.Views.Popups.AnimationEffects;
using GameMaker.UX.Views.Popups.ConditionEditor;
using GameMaker.UX.Views.Popups.EnemyArteEditor;
using GameMaker.UX.Views.Popups.EntityEditor;
using GameMaker.UX.Views.Popups.FrameSpanning;
using GameMaker.UX.Views.Popups.GameStart;
using GameMaker.UX.Views.Popups.MapIdConflict;
using GameMaker.UX.Views.Popups.MapLayers;
using GameMaker.UX.Views.Popups.MapSettings;
using GameMaker.UX.Views.Popups.MovementEditor;
using GameMaker.UX.Views.Popups.MovementRouteEditor;
using GameMaker.UX.Views.Popups.Progress;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.Services.Navigation;

public partial class NavigationService(Func<Type, EngineTask> engineTaskFactory) : PropertyChangedUpdater, INavigationService
{
    #region Properties

    public Frame? TopFrame
    {
        get;
        set => SetField(ref field, value);
    }

    public Frame? ActiveFrame
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Fields

    private string? _appName;

    #endregion

    #region Functions

    public void SetTopBar<T>() where T : Page
    {
        TopFrame?.Navigate(typeof(T));
    }

    public void NavigateTo<T>() where T : Page
    {
        if (ActiveFrame?.Content is T) return;
        if (ActiveFrame?.Navigate(typeof(T)) != true) return;

        // the app's name is the window's own title, set in MainWindow.xaml
        _appName ??= MainWindow.Window.Title;
        MainWindow.Window.Title = $"{_appName} - {PageTitle(typeof(T).Name)}";
    }

    // the page's name as the top bar shows it: the class name without "Page", in words
    private static string PageTitle(string pageName) => pageName switch
    {
        "ItemsPage" => "Consumables",
        "MediaPlayerPage" => "Media",
        _ => string.Concat(pageName.Replace("Page", "").Select((x, i) => i > 0 && char.IsUpper(x) ? $" {x}" : $"{x}"))
    };

    public async Task ShowProgressPopup<T>(string? label) where T : EngineTask
    {
        var progressPopup = new ProgressPopup
        {
            EngineTask = engineTaskFactory.Invoke(typeof(T)),
            ProgressText = label ?? "Progress",
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        await progressPopup.ShowAsync();
    }

    public async Task ShowAnimationEffectsPopup(AnimationEffectsPopupViewModel viewModel)
    {
        var popup = new AnimationEffectsPopup(viewModel)
        {
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        await popup.ShowAsync();
    }

    public async Task<ContentDialogResult> ShowFrameSpanningPopup(FrameSpanningPopupViewModel viewModel)
    {
        var popup = new FrameSpanningPopup(viewModel)
        {
            XamlRoot = MainWindow.Window.Content.XamlRoot
        };
        return await popup.ShowAsync();
    }

    public async Task ShowEntityEditorPopup(EntityEditorViewModel viewModel)
    {
        if (PopupHost.Current is null) return;
        await PopupHost.Current.ShowAsync(new EntityEditorPopup(viewModel));
    }

    public async Task ShowMovementRouteEditorPopup(MovementRouteEditorViewModel viewModel)
    {
        if (PopupHost.Current is null) return;
        await PopupHost.Current.ShowAsync(new MovementRouteEditorPopup(viewModel));
    }

    public async Task<ContentDialogResult> ShowMovementEditorPopup(MovementEditorViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new MovementEditorPopup(viewModel));
    }

    public async Task<ContentDialogResult> ShowConditionEditorPopup(ConditionEditorViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new ConditionEditorPopup(viewModel));
    }

    public async Task<ContentDialogResult> ShowActionEditorPopup(ActionEditorViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new ActionEditorPopup(viewModel));
    }

    public async Task<ContentDialogResult> ShowBranchActionEditorPopup(BranchActionEditorViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new BranchActionEditorPopup(viewModel));
    }

    public async Task<ContentDialogResult> ShowEnemyArteEditorPopup(EnemyArteEditorViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new EnemyArteEditorPopup(viewModel));
    }

    public async Task<ContentDialogResult> ShowMapSettingsPopup(MapSettingsViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new MapSettingsPopup(viewModel));
    }

    public async Task ShowMapLayersPopup(MainPageViewModel viewModel)
    {
        if (PopupHost.Current is null) return;
        await PopupHost.Current.ShowAsync(new MapLayersPopup(viewModel));
    }

    public async Task ShowAreaEditorPopup(MainPageViewModel viewModel)
    {
        if (PopupHost.Current is null) return;
        await PopupHost.Current.ShowAsync(new AreaEditorPopup(viewModel));
    }

    public async Task ShowPropEditorPopup(MainPageViewModel viewModel)
    {
        if (PopupHost.Current is null) return;
        await PopupHost.Current.ShowAsync(new PropEditorPopup(viewModel));
    }

    public async Task ShowPathEditorPopup(MainPageViewModel viewModel)
    {
        if (PopupHost.Current is null) return;
        await PopupHost.Current.ShowAsync(new PathEditorPopup(viewModel));
    }

    public async Task<ContentDialogResult> ShowMapIdConflictPopup(MapIdConflictViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new MapIdConflictPopup(viewModel));
    }

    public async Task ShowGameStartPopup(GameStartViewModel viewModel)
    {
        if (PopupHost.Current is null) return;
        await PopupHost.Current.ShowAsync(new GameStartPopup(viewModel));
    }

    public async Task<string?> PickFile(params string[] extensions)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        foreach (var extension in extensions) picker.FileTypeFilter.Add(extension);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.Window));

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<ContentDialogResult> ShowFillPropsPopup(FillPropsViewModel viewModel)
    {
        if (PopupHost.Current is null) return ContentDialogResult.None;
        return await PopupHost.Current.ShowAsync(new FillPropsPopup(viewModel));
    }

    public async Task<bool> Confirm(string title, string message, string yes)
    {
        if (MainWindow.Window.Content?.XamlRoot is not { } root) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap },
            PrimaryButtonText = yes,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = root
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public async Task ShowMessage(string title, string message)
    {
        if (MainWindow.Window.Content?.XamlRoot is not { } root) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap },
            CloseButtonText = "OK",
            XamlRoot = root
        };
        await dialog.ShowAsync();
    }

    #endregion
}
