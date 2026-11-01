using GameLibrary.Enumerations;
using GameLibrary.Models;
using GameLibrary.Models.Modules;
using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.ImageComponents;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// An entity placed on the map. The position lives in the entity's PositionModules (one per page),
/// the first one found is the editor position and moving the entity writes to every one of them.
/// </summary>
public partial class MapEntityModel(Entity entity, int tileSize) : PropertyChangedUpdater
{
    #region Properties

    public Entity Entity => entity;

    public string Name => entity.Name;

    public int Size => tileSize;

    public double X => Position?.X ?? 0;

    public double Y => Position?.Y ?? 0;

    public Direction Direction => Position?.Direction ?? Direction.Down;

    // the z the entity is placed at, written to every page so the entity keeps its depth whichever page is active
    public double Z
    {
        get => Position?.Z ?? 0;
        set
        {
            if (Z == value) return;
            foreach (var position in Positions()) position.Z = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DrawZ));
        }
    }

    // the z the engine draws at (z + sortZ)
    public double DrawZ => (Position?.Z ?? 0) + (Position?.SortZ ?? 0);

    public string PositionText => $"{(int) (X / tileSize)}, {(int) (Y / tileSize)}";

    // the character sheet shown on the map, taken from the first page that has one
    public CharacterImageModule? CharacterImage => Pages
        .SelectMany(x => x.Modules)
        .OfType<CharacterImageModule>()
        .FirstOrDefault(x => !string.IsNullOrEmpty(x.ImageName));

    public CroppedImage? Sprite
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(HasSprite));
        }
    }

    public bool HasSprite => Sprite is not null;

    // the canvas sprite drawing this entity, owned by the map scene
    public PictureBox? Picture { get; set; }

    public bool IsSelected
    {
        get;
        set => SetField(ref field, value);
    }

    private IEnumerable<Page> Pages => Enumerable.OrderBy(entity.Pages, x => x.Id);

    private PositionModule? Position => Pages.SelectMany(x => x.Modules).OfType<PositionModule>().FirstOrDefault();

    #endregion

    #region Methods

    public bool Contains(double worldX, double worldY)
    {
        return worldX >= X && worldX < X + tileSize && worldY >= Y && worldY < Y + tileSize;
    }

    public void MoveTo(double x, double y)
    {
        foreach (var position in Positions())
        {
            position.X = x;
            position.Y = y;
        }

        Refresh();
    }

    // call after the entity editor closes, pages and modules may have changed
    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(PositionText));
        OnPropertyChanged(nameof(Z));
        OnPropertyChanged(nameof(DrawZ));
    }

    #endregion

    #region Private Methods

    // every page's position module, adding one to the first page when the entity has none yet
    private PositionModule[] Positions()
    {
        var positions = entity.Pages.SelectMany(p => p.Modules).OfType<PositionModule>().ToArray();
        if (positions.Length > 0 || Pages.FirstOrDefault() is not { } page) return positions;

        var position = new PositionModule();
        page.Modules.Add(position);
        return [position];
    }

    #endregion
}
