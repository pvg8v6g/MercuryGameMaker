using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using Windows.Foundation;
using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Areas;
using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using GameLibrary.Services.GameData;
using GameLibrary.Services.Graphics;
using GameLibrary.Services.Json;
using GameLibrary.Services.Location;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.Services.Navigation;
using GameMaker.UX.Models.ActorsPage;
using GameMaker.UX.Models.EnemiesPage;
using GameMaker.UX.ViewModels.Popups.EnemyArteEditor;
using MercuryLibrary.Extensions;
using ContentDialogResult = Microsoft.UI.Xaml.Controls.ContentDialogResult;

namespace GameMaker.UX.ViewModels.EnemiesPage;

public partial class EnemiesPageViewModel(
    IGameDataService gameDataService,
    IJsonService jsonService,
    IGraphicsService graphicsService,
    ILocationService locationService,
    INavigationService navigationService) : BaseViewModel<Fighter>(jsonService)
{
    #region Properties

    public IGameDataService GameDataService => gameDataService;

    public string CharacterFolderPath => graphicsService.GetCharacterPath();

    public string EnemyFolderPath => Path.Combine(locationService.GraphicsDirectory!, "Enemies");

    public int HitboxViewGrid => 144;

    public ObservableCollection<EnemyAttributeViewModel> AttributeStats { get; } = [];

    public ObservableCollection<ActorElementModel> ElementResistanceStats { get; } = [];

    public Item[] AllItems =>
    [
        .. GameDataService.Consumables
            .Cast<Item>()
            .Concat(GameDataService.Equipment)
            .OrderBy(x => x.Name)
    ];

    protected override ObservableCollection<Fighter> EntityCollection => gameDataService.Enemies;

    public CroppedImage? AnchorImage
    {
        get;
        private set => SetField(ref field, value);
    }

    /*
     * selected direction of the hitbox viewer - independent of the character direction
     */
    public Direction SelectedDirection
    {
        get;
        set
        {
            SetField(ref field, value);
            _ = RefreshAnchor();
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
            RefreshHitboxes();
        }
    } = [];

    public ObservableCollection<Area> PreviewsHitboxes { get; } = [];

    public double HitboxPreviewScale
    {
        get;
        private set => SetField(ref field, value);
    } = 1.0;

    public double HitboxPreviewOffsetX
    {
        get;
        private set => SetField(ref field, value);
    }

    public double HitboxPreviewOffsetY
    {
        get;
        private set => SetField(ref field, value);
    }

    public string? CharacterName
    {
        get;
        set => SetField(ref field, value);
    }

    /*
     * true index - disregarding direction
     */
    public int CharacterIndex
    {
        get;
        set => SetField(ref field, value);
    }

    public ObservableCollection<EnemyArteModel> EnemyArtes { get; } = [];

    public int SelectedEnemyArteIndex
    {
        get;
        set => SetField(ref field, value);
    } = -1;

    public FighterArte? CopiedEnemyArte
    {
        get;
        private set => SetField(ref field, value);
    }

    #endregion

    #region Commands

    public RelayCommand<SpriteSelectedArgs> SelectSpriteCommand => new(SelectSpriteAction);

    public AsyncRelayCommand NewArteCommand => field ??= new AsyncRelayCommand(NewArteAction);

    public AsyncRelayCommand EditArteCommand => field ??= new AsyncRelayCommand(EditArteAction);

    public RelayCommand CopyArteCommand => field ??= new RelayCommand(CopyArteAction);

    public RelayCommand PasteArteCommand => field ??= new RelayCommand(PasteArteAction);

    public RelayCommand DeleteArteCommand => field ??= new RelayCommand(DeleteArteAction);

    #endregion

    #region Fields

    private Rect? _spriteBox;

    private double SpriteWidth => _spriteBox?.Width * HitboxPreviewScale ?? 0.0;

    private double SpriteHeight => _spriteBox?.Height * HitboxPreviewScale ?? 0.0;

    #endregion

    #region Actions

    private void SelectSpriteAction(SpriteSelectedArgs args)
    {
        if (SelectedEntity is null) return;
        SelectedEntity.CharacterName = args.FileName;
        SelectedEntity.CharacterIndex = args.Index;
        SelectedEntity.CharacterDirection = args.Direction;
        _ = RefreshAnchor();
        _ = RefreshCharacter();
    }

    protected override async Task OnSelectedIndexChanged(int selectedIndex)
    {
        RefreshStats();
        RefreshArtes();
        SelectedDirection = Direction.None;
        await RefreshAnchor();
        await RefreshCharacter();
    }

    private void OnHitboxesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshHitboxes();
    }

    private async Task NewArteAction()
    {
        if (SelectedEntity is null) return;

        var fighterArte = new FighterArte();
        var result = await navigationService.ShowEnemyArteEditorPopup(new EnemyArteEditorViewModel(fighterArte, gameDataService, navigationService));
        if (result != ContentDialogResult.Primary || fighterArte.ArteGuid is null) return;

        SelectedEntity.ConditionalArtes.Add(fighterArte);
        RefreshArtes();
        SelectedEnemyArteIndex = EnemyArtes.Count - 1;
    }

    // edits a copy so cancelling leaves the original untouched
    private async Task EditArteAction()
    {
        if (SelectedEntity is null || SelectedEnemyArteIndex < 0 || SelectedEnemyArteIndex >= EnemyArtes.Count) return;

        var index = SelectedEnemyArteIndex;
        var copy = CloneArte(EnemyArtes[index].FighterArte);
        if (copy is null) return;

        var result = await navigationService.ShowEnemyArteEditorPopup(new EnemyArteEditorViewModel(copy, gameDataService, navigationService));
        if (result != ContentDialogResult.Primary || copy.ArteGuid is null) return;

        SelectedEntity.ConditionalArtes[index] = copy;
        RefreshArtes();
        SelectedEnemyArteIndex = index;
    }

    private void CopyArteAction()
    {
        if (SelectedEnemyArteIndex < 0 || SelectedEnemyArteIndex >= EnemyArtes.Count) return;
        CopiedEnemyArte = EnemyArtes[SelectedEnemyArteIndex].FighterArte;
    }

    private void PasteArteAction()
    {
        if (SelectedEntity is null || CopiedEnemyArte is null) return;

        var clone = CloneArte(CopiedEnemyArte);
        if (clone is null) return;

        foreach (var condition in clone.Conditions) condition.Guid = Guid.NewGuid();
        SelectedEntity.ConditionalArtes.Add(clone);
        RefreshArtes();
        SelectedEnemyArteIndex = EnemyArtes.Count - 1;
    }

    private void DeleteArteAction()
    {
        if (SelectedEntity is null || SelectedEnemyArteIndex < 0 || SelectedEnemyArteIndex >= EnemyArtes.Count) return;

        var index = SelectedEnemyArteIndex;
        SelectedEntity.ConditionalArtes.RemoveAt(index);
        RefreshArtes();
        SelectedEnemyArteIndex = Math.Min(index, EnemyArtes.Count - 1);
    }

    #endregion

    #region Private Methods

    private void RefreshArtes()
    {
        SelectedEnemyArteIndex = -1;
        EnemyArtes.Clear();
        if (SelectedEntity is null) return;

        foreach (var fighterArte in SelectedEntity.ConditionalArtes)
        {
            var arte = gameDataService.Artes.FirstOrDefault(x => x.Guid == fighterArte.ArteGuid);
            EnemyArtes.Add(new EnemyArteModel(fighterArte, arte));
        }
    }

    private static FighterArte? CloneArte(FighterArte fighterArte)
    {
        return JsonSerializer.Deserialize<FighterArte>(JsonSerializer.Serialize(fighterArte));
    }

    private async Task RefreshAnchor()
    {
        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty())
        {
            AnchorImage = null;
            return;
        }

        var direction = SelectedDirection is Direction.None ? Direction.Down : SelectedDirection;
        AnchorImage = await graphicsService.GetCharacter(SelectedEntity!.CharacterName!, SelectedEntity.CharacterIndex, direction);
    }

    // sizes the sprite box and links the hitboxes to the selected entity
    private async Task RefreshCharacter()
    {
        CharacterName = SelectedEntity?.CharacterName;
        if (SelectedEntity is null || CharacterName.IsNullOrEmpty())
        {
            _spriteBox = null;
            SelectedDirectionHitboxes = [];
            CharacterIndex = 1;
            return;
        }

        _spriteBox = (await graphicsService.GetCharacter(SelectedEntity.CharacterName!, SelectedEntity.CharacterIndex, SelectedDirection)).Rect;
        LinkHitboxes(SelectedDirection);
        CharacterIndex = graphicsService.GetCharacterIndexFromDirection(
            SelectedEntity.CharacterName!,
            SelectedEntity.CharacterIndex,
            SelectedEntity.CharacterDirection);
    }

    private void RefreshStats()
    {
        AttributeStats.Clear();
        ElementResistanceStats.Clear();

        if (SelectedEntity is null) return;

        foreach (var attribute in GameDataService.Attributes)
        {
            AttributeStats.Add(new EnemyAttributeViewModel(attribute, SelectedEntity));
        }

        foreach (var element in GameDataService.Elements)
        {
            ElementResistanceStats.Add(new ActorElementModel(element, SelectedEntity));
        }
    }

    private void LinkHitboxes(Direction direction)
    {
        if (SelectedEntity is null)
        {
            SelectedDirectionHitboxes = [];
        }
        else
        {
            SelectedEntity.Hitboxes.TryAdd(direction, []);
            SelectedDirectionHitboxes = SelectedEntity.Hitboxes[direction];
        }
    }

    private void RefreshHitboxes()
    {
        PreviewsHitboxes.Clear();
        CalculateSpriteDefaultOffset();

        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty()) return;
        Area[] hitboxes = [];
        if (SelectedDirection is not Direction.None) hitboxes = [..SelectedEntity.Hitboxes.GetValueOrDefault(SelectedDirection, [])];
        hitboxes = [..hitboxes, ..SelectedEntity.Hitboxes.GetValueOrDefault(Direction.None, [])];
        if (hitboxes.Length == 0)
        {
            HitboxPreviewScale = 1.0;
            CalculateSpriteDefaultOffset();
            return;
        }

        foreach (var area in hitboxes)
        {
            PreviewsHitboxes.Add(area);
        }

        if (_spriteBox is null)
        {
            HitboxPreviewOffsetX = 0.0;
            HitboxPreviewOffsetY = 0.0;
            HitboxPreviewScale = 1.0;
            return;
        }

        var characterSprite = new Area { OffsetX = 0, OffsetY = 0, Width = (int) SpriteWidth, Height = (int) SpriteHeight };
        Area[] previews = [..PreviewsHitboxes, characterSprite];
        var left = previews.Select(x => x.OffsetX).Min();
        var right = previews.Select(x => x.OffsetX + x.Width).Max();
        var top = previews.Select(x => x.OffsetY).Min();
        var bottom = previews.Select(x => x.OffsetY + x.Height).Max();

        var fullWidth = right - left;
        var fullHeight = bottom - top;
        var max = Math.Max(fullWidth, fullHeight);
        var scale = HitboxViewGrid / (double) max;
        if (scale > 1.0)
        {
            HitboxPreviewOffsetX = (HitboxViewGrid - fullWidth) / 2.0 - left;
            HitboxPreviewOffsetY = (HitboxViewGrid - fullHeight) / 2.0 - top;
            HitboxPreviewScale = 1.0;
        }
        else
        {
            HitboxPreviewOffsetX = (HitboxViewGrid - fullWidth * scale) / 2.0 - left * scale;
            HitboxPreviewOffsetY = (HitboxViewGrid - fullHeight * scale) / 2.0 - top * scale;
            HitboxPreviewScale = scale;
        }
    }

    // reset offset to center
    private void CalculateSpriteDefaultOffset()
    {
        if (SelectedEntity is null || SelectedEntity.CharacterName.IsNullOrEmpty() || _spriteBox is null)
        {
            HitboxPreviewOffsetX = 0.0;
            HitboxPreviewOffsetY = 0.0;
            return;
        }

        HitboxPreviewOffsetX = (HitboxViewGrid - SpriteWidth) / 2.0;
        HitboxPreviewOffsetY = (HitboxViewGrid - SpriteHeight) / 2.0;
    }

    #endregion
}

