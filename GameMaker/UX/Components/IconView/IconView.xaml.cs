using GameLibrary.Models;
using GameLibrary.Utilities;
using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.AppMain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameMaker.UX.Components.IconView;

/// <summary>
/// Shows an icon and picks a new one: the flyout has a dropdown of the icon sheets in Graphics\Icons and the chosen
/// sheet below it. Clicking a cell picks that icon on that sheet.
/// </summary>
public partial class IconView
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(GameIcon), typeof(IconView), new PropertyMetadata(GameIcon.None, OnIconChanged));

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not IconView iconView) return;
        iconView.UpdateSelectedBorder();
        iconView.UpdateCroppedImage().Forget();
    }

    public GameIcon Icon
    {
        get => (GameIcon) GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public CroppedImage CroppedImage { get; } = new();

    // the sheet the flyout is showing, which isn't always the sheet of the current icon
    private string? _shownSheet;

    private static IGraphicsService GraphicsService => App.Services!.GetRequiredService<IGraphicsService>();

    public IconView()
    {
        InitializeComponent();
        IconFlyout.Opened += IconFlyout_Opened;
        Loaded += (_, _) => UpdateCroppedImage().Forget();
    }

    private async Task UpdateCroppedImage()
    {
        var icon = await GraphicsService.GetIcon(Icon ?? GameIcon.None);
        CroppedImage.ImageSource = icon.ImageSource;
        CroppedImage.Rect = icon.Rect;
        UpdateSelectedBorder();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
    }

    // opens on the current icon's sheet, the sheet list is read fresh so new sheets show up
    private void IconFlyout_Opened(object? sender, object e)
    {
        var sheets = GraphicsService.GetIconSheets();
        SheetBox.ItemsSource = sheets.Select(x => new SheetChoice(x)).ToArray();
        var current = Icon?.Sheet ?? sheets.FirstOrDefault();
        var shown = current is not null && sheets.Contains(current) ? current : sheets.FirstOrDefault();
        SheetBox.SelectedItem = shown is null ? null : new SheetChoice(shown);
        LoadSheet(shown).Forget();
    }

    private void SheetBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SheetBox.SelectedItem is SheetChoice choice && choice.File != _shownSheet) LoadSheet(choice.File).Forget();
    }

    private async Task LoadSheet(string? sheet)
    {
        _shownSheet = sheet;
        var bitmapImage = await GraphicsService.GetImage(GraphicsService.GetIconSheetPath(sheet));

        if (bitmapImage.PixelWidth == 0)
        {
            bitmapImage.ImageOpened += (_, _) =>
            {
                SizeToSheet();
                UpdateSelectedBorder();
                ScrollToSelected();
            };
        }

        FullIconsImage.Source = bitmapImage;
        SizeToSheet();
        UpdateSelectedBorder();
        ScrollToSelected();
    }

    // the current icon is on the shown sheet (an icon without a sheet is on the first one)
    private bool CurrentIconIsShown()
    {
        var sheets = GraphicsService.GetIconSheets();
        var iconSheet = Icon?.Sheet ?? sheets.FirstOrDefault();
        return iconSheet is not null && string.Equals(iconSheet, _shownSheet, StringComparison.OrdinalIgnoreCase);
    }

    private void ScrollToSelected()
    {
        if (CroppedImage.Rect == null || !CurrentIconIsShown())
        {
            IconScrollViewer.ChangeView(0, 0, null);
            return;
        }

        // the layout has to be up to date for the viewport size to be right
        IconScrollViewer.UpdateLayout();
        var y = CroppedImage.Rect.Value.Y;
        var offsetY = y - IconScrollViewer.ViewportHeight / 2 + CroppedImage.Rect.Value.Height / 2;
        IconScrollViewer.ChangeView(0, offsetY, null);
    }

    private void SizeToSheet()
    {
        if (FullIconsImage.Source is not BitmapImage { PixelWidth: > 0 } bitmapImage) return;

        OverlayCanvas.Width = bitmapImage.PixelWidth;
        OverlayCanvas.Height = bitmapImage.PixelHeight;
        IconsContainer.Width = bitmapImage.PixelWidth;
        IconsContainer.Height = bitmapImage.PixelHeight;
        FullIconsImage.Width = bitmapImage.PixelWidth;
        FullIconsImage.Height = bitmapImage.PixelHeight;
    }

    private void UpdateSelectedBorder()
    {
        if (CroppedImage.Rect == null || !CurrentIconIsShown())
        {
            SelectedBorder.Visibility = Visibility.Collapsed;
            return;
        }

        Canvas.SetLeft(SelectedBorder, (int) CroppedImage.Rect.Value.X);
        Canvas.SetTop(SelectedBorder, (int) CroppedImage.Rect.Value.Y);
        SelectedBorder.Visibility = Visibility.Visible;
    }

    private void IconsContainer_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var w = GraphicsService.IconSize;
        var h = GraphicsService.IconSize;
        var position = e.GetCurrentPoint(IconsContainer).Position;

        Canvas.SetLeft(HoverBorder, (int) (position.X / w) * w);
        Canvas.SetTop(HoverBorder, (int) (position.Y / h) * h);
        HoverBorder.Visibility = Visibility.Visible;
    }

    private void IconsContainer_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        HoverBorder.Visibility = Visibility.Collapsed;
    }

    private void FullIconsImage_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (FullIconsImage.Source is not BitmapImage bitmapImage || _shownSheet is null) return;

        var w = GraphicsService.IconSize;
        var h = GraphicsService.IconSize;
        var position = e.GetPosition(IconsContainer);
        var columns = (int) (bitmapImage.PixelWidth / w);
        var index = (int) (position.Y / h) * columns + (int) (position.X / w);
        Icon = new GameIcon(_shownSheet, index);
        IconFlyout.Hide();
    }
}

// a sheet in the dropdown: shown by its theme name (the file name without .png), equal by file
internal sealed record SheetChoice(string File)
{
    public override string ToString() => Path.GetFileNameWithoutExtension(File);
}
