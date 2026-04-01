using System.Collections.ObjectModel;
using Windows.Foundation;
using GameLibrary.Models.Areas;
using GameLibrary.Utilities.ComponentModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace GameMaker.UX.Components.GridArea;

public partial class AreaCreatorView
{
    #region Registered Dependencies

    public static readonly DependencyProperty HitboxesProperty = DependencyProperty.Register(
        nameof(Hitboxes), typeof(ObservableCollection<Hitbox>), typeof(GridAreaView), new PropertyMetadata(null, OnHitboxesChanged));

    public ObservableCollection<Hitbox>? Hitboxes
    {
        get => (ObservableCollection<Hitbox>?) GetValue(HitboxesProperty);
        set => SetValue(HitboxesProperty, value);
    }

    public static readonly DependencyProperty CharacterImageProperty = DependencyProperty.Register(nameof(CharacterImage), typeof(CroppedImage),
        typeof(GridAreaView), new PropertyMetadata(null, OnCharacterImageChanged));

    public CroppedImage? CharacterImage
    {
        get => (CroppedImage?) GetValue(CharacterImageProperty);
        set => SetValue(CharacterImageProperty, value);
    }

    #endregion

    #region Properties

    private Point? StartPosition { get; set; }

    private Point? AnchorPosition { get; set; }

    public int GridSize { get; set; } = 21;

    public int BoxSize { get; set; } = 48;

    public int InternalGridSize => GridSize * BoxSize;

    public bool ShowGrid { get; set; } = true;

    #endregion

    #region Constructor

    public AreaCreatorView()
    {
        InitializeComponent();
    }

    #endregion

    #region Listeners

    private static void OnHitboxesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AreaCreatorView control)
        {
        }
    }

    private static void OnCharacterImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AreaCreatorView control)
        {
        }
    }

    private void OnClearAllClick(object sender, RoutedEventArgs e)
    {
        Hitboxes?.Clear();
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
    }

    #endregion
}
