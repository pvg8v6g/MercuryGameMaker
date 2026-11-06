using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.Foundation;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Services.Graphics;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.Hitboxes;

/// <summary>
/// Edits a character's hitboxes per direction the same way the actors page does: the character is drawn
/// facing the chosen direction, None hitboxes count for every direction, and the preview zooms out to fit them all.
/// </summary>
public partial class HitboxEditorModel(IGraphicsService graphicsService) : PropertyChangedUpdater
{
    #region Fields

    private Dictionary<Direction, ObservableCollection<Area>>? _hitboxes;
    private string? _characterName;
    private int _characterIndex;
    private Rect? _spriteBox;

    #endregion

    #region Properties

    // the size of the preview box
    public int ViewGrid => 144;

    public CroppedImage? AnchorImage
    {
        get;
        private set => SetField(ref field, value);
    }

    // which direction's hitboxes are being edited, independent of the way the character faces
    public Direction SelectedDirection
    {
        get;
        set
        {
            SetField(ref field, value);
            _ = Refresh();
        }
    } = Direction.None;

    public ObservableCollection<Area> SelectedDirectionHitboxes
    {
        get;
        set
        {
            field.CollectionChanged -= OnHitboxesCollectionChanged;
            SetField(ref field, value);
            field.CollectionChanged += OnHitboxesCollectionChanged;
            RefreshPreview();
        }
    } = [];

    public ObservableCollection<Area> PreviewsHitboxes { get; } = [];

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

    private bool HasCharacter => !string.IsNullOrEmpty(_characterName);

    private double SpriteWidth => _spriteBox?.Width * PreviewScale ?? 0.0;

    private double SpriteHeight => _spriteBox?.Height * PreviewScale ?? 0.0;

    #endregion

    #region Methods

    // the hitboxes to edit and the character they belong to (index is the sheet column)
    public Task Show(Dictionary<Direction, ObservableCollection<Area>>? hitboxes, string? characterName, int characterIndex)
    {
        _hitboxes = hitboxes;
        _characterName = characterName;
        _characterIndex = characterIndex;
        return Refresh();
    }

    #endregion

    #region Private Methods

    private async Task Refresh()
    {
        if (_hitboxes is null || !HasCharacter)
        {
            AnchorImage = null;
            _spriteBox = null;
            SelectedDirectionHitboxes = [];
            return;
        }

        var direction = SelectedDirection is Direction.None ? Direction.Down : SelectedDirection;
        AnchorImage = await graphicsService.GetCharacter(_characterName!, _characterIndex, direction);
        _spriteBox = AnchorImage.Rect;

        _hitboxes.TryAdd(SelectedDirection, []);
        SelectedDirectionHitboxes = _hitboxes[SelectedDirection];
    }

    private void OnHitboxesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        PreviewsHitboxes.Clear();
        CenterSprite();

        if (_hitboxes is null || !HasCharacter) return;

        Area[] hitboxes = [];
        if (SelectedDirection is not Direction.None) hitboxes = [.. _hitboxes.GetValueOrDefault(SelectedDirection, [])];
        hitboxes = [.. hitboxes, .. _hitboxes.GetValueOrDefault(Direction.None, [])];
        if (hitboxes.Length == 0)
        {
            PreviewScale = 1.0;
            CenterSprite();
            return;
        }

        foreach (var area in hitboxes) PreviewsHitboxes.Add(area);

        if (_spriteBox is null)
        {
            PreviewOffsetX = 0.0;
            PreviewOffsetY = 0.0;
            PreviewScale = 1.0;
            return;
        }

        // zoom out until the character and every hitbox fit in the preview
        var characterSprite = new Area { OffsetX = 0, OffsetY = 0, Width = (int) SpriteWidth, Height = (int) SpriteHeight };
        Area[] previews = [.. PreviewsHitboxes, characterSprite];
        var left = previews.Select(x => x.OffsetX).Min();
        var right = previews.Select(x => x.OffsetX + x.Width).Max();
        var top = previews.Select(x => x.OffsetY).Min();
        var bottom = previews.Select(x => x.OffsetY + x.Height).Max();

        var fullWidth = right - left;
        var fullHeight = bottom - top;
        var scale = ViewGrid / (double) Math.Max(fullWidth, fullHeight);
        if (scale > 1.0)
        {
            PreviewOffsetX = ((ViewGrid - fullWidth) / 2.0) - left;
            PreviewOffsetY = ((ViewGrid - fullHeight) / 2.0) - top;
            PreviewScale = 1.0;
        }
        else
        {
            PreviewOffsetX = ((ViewGrid - (fullWidth * scale)) / 2.0) - (left * scale);
            PreviewOffsetY = ((ViewGrid - (fullHeight * scale)) / 2.0) - (top * scale);
            PreviewScale = scale;
        }
    }

    private void CenterSprite()
    {
        if (!HasCharacter || _spriteBox is null)
        {
            PreviewOffsetX = 0.0;
            PreviewOffsetY = 0.0;
            return;
        }

        PreviewOffsetX = (ViewGrid - SpriteWidth) / 2.0;
        PreviewOffsetY = (ViewGrid - SpriteHeight) / 2.0;
    }

    #endregion
}
