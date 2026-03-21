using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.AppMain;
using MercuryLibrary.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

// ReSharper disable PossibleLossOfFraction

namespace GameMaker.UX.Components.ImageChooser;

public partial class ImageChooser
{
    public static readonly DependencyProperty FolderPathProperty = DependencyProperty.Register(
        nameof(FolderPath), typeof(string), typeof(ImageChooser), new PropertyMetadata(string.Empty, OnFolderPathChanged));

    public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(
        nameof(FileName), typeof(string), typeof(ImageChooser), new PropertyMetadata(null, OnFileNameChanged));

    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(
        nameof(Index), typeof(int), typeof(ImageChooser), new PropertyMetadata(0, OnIndexChanged));

    public static readonly DependencyProperty ChooserWidthProperty = DependencyProperty.Register(
        nameof(ChooserWidth), typeof(double), typeof(ImageChooser), new PropertyMetadata(48.0));

    public static readonly DependencyProperty ChooserHeightProperty = DependencyProperty.Register(
        nameof(ChooserHeight), typeof(double), typeof(ImageChooser), new PropertyMetadata(48.0));

    private static void OnFolderPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageChooser control) control.LoadFiles();
    }

    private static async void OnFileNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageChooser control) await control.UpdateCroppedImage();
    }

    private static async void OnIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ImageChooser control) await control.UpdateCroppedImage();
    }

    public string FolderPath
    {
        get => (string) GetValue(FolderPathProperty);
        set => SetValue(FolderPathProperty, value);
    }

    public string? FileName
    {
        get => (string?) GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    public int Index
    {
        get => (int) GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public double ChooserWidth
    {
        get => (double) GetValue(ChooserWidthProperty);
        set => SetValue(ChooserWidthProperty, value);
    }

    public double ChooserHeight
    {
        get => (double) GetValue(ChooserHeightProperty);
        set => SetValue(ChooserHeightProperty, value);
    }

    private int _updateTicket;

    public CroppedImage CroppedImage { get; } = new();

    public ImageChooser()
    {
        InitializeComponent();
        ImageFlyout.Opened += ImageFlyout_Opened;
        Loaded += async (_, _) => await UpdateCroppedImage();
    }

    private void LoadFiles()
    {
        if (FolderPath.IsNullOrEmpty()) return;
        try
        {
            var files = Directory.GetFiles(FolderPath, "*.png")
                .Select(Path.GetFileName)
                .ToList();
            FileListView.ItemsSource = files;
        }
        catch
        {
            // Ignore folder access errors for now
        }
    }

    private async Task UpdateCroppedImage()
    {
        var ticket = ++_updateTicket;

        if (FileName.IsNullOrEmpty() || FolderPath.IsNullOrEmpty())
        {
            if (ticket != _updateTicket) return;
            CroppedImage.ImageSource = null;
            CroppedImage.Rect = null;
            UpdateSelectedBorder();
            return;
        }

        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var path = Path.IsPathRooted(FileName) ? FileName : Path.Combine(FolderPath, FileName);

        var source = await graphicsService.GetImage(path);
        if (ticket != _updateTicket) return;

        var (w, h) = await graphicsService.GetSegmentation(path);
        if (ticket != _updateTicket) return;

        if (source.PixelWidth == 0)
        {
            source.ImageOpened += async (_, _) =>
            {
                if (ticket != _updateTicket) return;
                var dims = await GetImageDimensions(path);
                if (ticket != _updateTicket) return;

                var cols = (int) (dims.width / w);
                var xPos = (Index % cols) * w;
                var yPos = (Index / cols) * h;
                CroppedImage.Rect = new Rect(xPos, yPos, w, h);
                UpdateSelectedBorder();
            };
        }

        var dimensions = await GetImageDimensions(path);
        if (ticket != _updateTicket) return;

        var columns = (int) (dimensions.width / w);
        var x = (Index % columns) * w;
        var y = (Index / columns) * h;

        CroppedImage.ImageSource = source;
        CroppedImage.Rect = new Rect(x, y, w, h);
        UpdateSelectedBorder();
    }

    private async Task<(double width, double height)> GetImageDimensions(string imagePath)
    {
        if (!File.Exists(imagePath)) return (0, 0);
        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        return (decoder.PixelWidth, decoder.PixelHeight);
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        FlyoutBase.ShowAttachedFlyout(RootGrid);
    }

    private async void ImageFlyout_Opened(object? sender, object e)
    {
        if (FileListView.ItemsSource == null) LoadFiles();
        if (FileListView.SelectedItem == null && !FileName.IsNullOrEmpty())
        {
            FileListView.SelectedItem = FileName;
        }

        await LoadFullImage();
        UpdateSelectedBorder();
        ScrollToSelected();
    }

    private async Task LoadFullImage()
    {
        if (FileName.IsNullOrEmpty())
        {
            FullImage.Source = null;
            UpdateSelectedBorder();
            return;
        }

        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var path = Path.IsPathRooted(FileName) ? FileName : Path.Combine(FolderPath, FileName);
        var bitmapImage = await graphicsService.GetImage(path);

        if (bitmapImage.PixelWidth == 0)
        {
            bitmapImage.ImageOpened += (_, _) =>
            {
                UpdateSelectedBorder();
                ScrollToSelected();
            };
        }

        FullImage.Source = bitmapImage;
    }

    private void FileListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileListView.SelectedItem is not string selectedFile) return;
        FileName = selectedFile;
        _ = LoadFullImage();
    }

    private void ScrollToSelected()
    {
        if (CroppedImage.Rect == null) return;
        var y = CroppedImage.Rect.Value.Y;
        ImageScrollViewer.UpdateLayout();
        var offsetY = y - ImageScrollViewer.ViewportHeight / 2 + CroppedImage.Rect.Value.Height / 2;
        ImageScrollViewer.ChangeView(0, offsetY, null);
    }

    private async void UpdateSelectedBorder()
    {
        if (FullImage.Source is BitmapImage bitmapImage)
        {
            if (bitmapImage.PixelWidth == 0 || CroppedImage.Rect == null) return;

            var rect = CroppedImage.Rect.Value;

            Canvas.SetLeft(SelectedBorder, rect.X);
            Canvas.SetTop(SelectedBorder, rect.Y);
            SelectedBorder.Width = rect.Width;
            SelectedBorder.Height = rect.Height;
            SelectedBorder.Visibility = Visibility.Visible;

            OverlayCanvas.Width = bitmapImage.PixelWidth;
            OverlayCanvas.Height = bitmapImage.PixelHeight;
            ImageContainer.Width = bitmapImage.PixelWidth;
            ImageContainer.Height = bitmapImage.PixelHeight;
            FullImage.Width = bitmapImage.PixelWidth;
            FullImage.Height = bitmapImage.PixelHeight;
        }
    }

    private async void ImageContainer_OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (FileName.IsNullOrEmpty()) return;
        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var path = Path.IsPathRooted(FileName) ? FileName : Path.Combine(FolderPath, FileName);
        var (w, h) = await graphicsService.GetSegmentation(path);
        var position = e.GetCurrentPoint(ImageContainer).Position;

        var x = (int) (position.X / w) * w;
        var y = (int) (position.Y / h) * h;

        Canvas.SetLeft(HoverBorder, x);
        Canvas.SetTop(HoverBorder, y);
        HoverBorder.Width = w;
        HoverBorder.Height = h;
        HoverBorder.Visibility = Visibility.Visible;
    }

    private void ImageContainer_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        HoverBorder.Visibility = Visibility.Collapsed;
    }

    private async void FullImage_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (FileName.IsNullOrEmpty()) return;
        var graphicsService = App.Services!.GetRequiredService<IGraphicsService>();
        var path = Path.IsPathRooted(FileName) ? FileName : Path.Combine(FolderPath, FileName);
        var (w, h) = await graphicsService.GetSegmentation(path);
        var position = e.GetPosition(ImageContainer);

        var xIndex = (int) (position.X / w);
        var yIndex = (int) (position.Y / h);

        if (FullImage.Source is not BitmapImage bitmapImage) return;
        var columns = (int) (bitmapImage.PixelWidth / w);

        Index = yIndex * columns + xIndex;

        ImageFlyout.Hide();
    }
}
