using GameLibrary.Utilities;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Props;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using MercuryLibrary.Extensions;
using GameLibrary.Utilities.ComponentModels;

namespace GameMaker.UX.ViewModels.PropsPage;

/// <summary>
/// The props catalogue: each prop's picture, its footprint (what blocks movement) and how it sorts with the characters.
/// </summary>
public class PropsPageViewModel(
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService) : BaseViewModel<Prop>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    // the footprint preview is drawn in a square this big
    public int FootprintViewGrid => 144;

    public string? ImageName
    {
        get => SelectedEntity?.ImageName;
        set
        {
            if (SelectedEntity is null || SelectedEntity.ImageName == value) return;
            SelectedEntity.ImageName = value;
            OnPropertyChanged();
            RefreshImage().Forget();
        }
    }

    public double BuryDepth
    {
        get => SelectedEntity?.BuryDepth ?? 0;
        set
        {
            if (SelectedEntity is null || double.IsNaN(value)) return;
            SelectedEntity.BuryDepth = (int) Math.Max(0, value);
            OnPropertyChanged();
            RefreshImage().Forget();
        }
    }

    public double BuryFeather
    {
        get => SelectedEntity?.BuryFeather ?? 0;
        set
        {
            if (SelectedEntity is null || double.IsNaN(value)) return;
            SelectedEntity.BuryFeather = (int) Math.Max(0, value);
            OnPropertyChanged();
            RefreshImage().Forget();
        }
    }

    public CroppedImage? PropImage
    {
        get;
        private set => SetField(ref field, value);
    }

    public string ImageSizeText
    {
        get;
        private set => SetField(ref field, value);
    } = string.Empty;

    public ObservableCollection<Area> Footprint
    {
        get;
        private set
        {
            field.CollectionChanged -= OnFootprintChanged;
            SetField(ref field, value);
            field.CollectionChanged += OnFootprintChanged;
            RefreshPreview();
        }
    } = [];

    public ObservableCollection<Area> PreviewFootprint { get; } = [];

    public double PreviewScale
    {
        get;
        private set => SetField(ref field, value);
    } = 1.0;

    public double PreviewOffsetX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double PreviewOffsetY
    {
        get;
        private set => SetField(ref field, value);
    }

    protected override ObservableCollection<Prop> EntityCollection => gameDataService.Props;

    // the image chooser lists every picture in Graphics\Props
    public string PropsFolderPath => Path.Combine(locationService.GraphicsDirectory!, "Props");

    #endregion

    #region Fields

    private Size? _imageSize;

    #endregion

    #region Commands

    // a starting footprint: a strip across the bottom middle of the picture, about where a trunk or a rock's base is
    public RelayCommand AddBaseFootprintCommand => new(AddBaseFootprint);

    #endregion

    #region Actions

    protected override async Task OnSelectedIndexChanged(int index)
    {
        OnPropertyChanged(nameof(ImageName));
        OnPropertyChanged(nameof(BuryDepth));
        OnPropertyChanged(nameof(BuryFeather));
        Footprint = SelectedEntity?.Footprint ?? [];
        await RefreshImage();
    }

    private void OnFootprintChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPreview();
    }

    private void AddBaseFootprint()
    {
        if (SelectedEntity is null || _imageSize is not { } size) return;

        var width = Math.Max(8, (int) (size.Width * 0.3));
        var height = Math.Max(6, (int) (size.Height * 0.08));
        SelectedEntity.Footprint.Add(new Area
        {
            OffsetX = (int) ((size.Width - width) / 2),
            OffsetY = (int) size.Height - height,
            Width = width,
            Height = height
        });
    }

    #endregion

    #region Private Methods

    private async Task RefreshImage()
    {
        _imageSize = null;
        PropImage = null;
        ImageSizeText = string.Empty;

        var name = SelectedEntity?.ImageName;
        var path = name.IsNullOrEmpty() ? null : Path.Combine(PropsFolderPath, name!);
        if (path is not null && File.Exists(path))
        {
            var (width, height) = await graphicsService.GetImageDimensions(path);
            _imageSize = new Size(width, height);
            ImageSizeText = $"{width:0} x {height:0} px";
            // shown the way the map draws it, with its bottom buried in the ground
            var drawn = await graphicsService.GetPropImagePath(path, false, SelectedEntity!.BuryDepth, SelectedEntity.BuryFeather);
            PropImage = new CroppedImage { ImagePath = drawn, ImageSource = await graphicsService.GetImage(drawn), Rect = new Rect(0, 0, width, height) };
        }

        RefreshPreview();
    }

    // fits the picture and every footprint box into the preview square
    private void RefreshPreview()
    {
        PreviewFootprint.Clear();
        foreach (var area in Footprint) PreviewFootprint.Add(area);

        if (_imageSize is not { } size)
        {
            PreviewScale = 1.0;
            PreviewOffsetX = 0.0;
            PreviewOffsetY = 0.0;
            return;
        }

        Area[] boxes = [.. PreviewFootprint, new Area { Width = (int) size.Width, Height = (int) size.Height }];
        var left = boxes.Min(x => x.OffsetX);
        var top = boxes.Min(x => x.OffsetY);
        var fullWidth = boxes.Max(x => x.OffsetX + x.Width) - left;
        var fullHeight = boxes.Max(x => x.OffsetY + x.Height) - top;
        var scale = Math.Min(1.0, FootprintViewGrid / (double) Math.Max(fullWidth, fullHeight));

        PreviewScale = scale;
        PreviewOffsetX = (FootprintViewGrid - fullWidth * scale) / 2.0 - left * scale;
        PreviewOffsetY = (FootprintViewGrid - fullHeight * scale) / 2.0 - top * scale;
    }

    #endregion
}
