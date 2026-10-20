using System.Collections.ObjectModel;
using GameLibrary.Models.Areas;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.UX.Components.GridArea;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.HitboxPreview;

public partial class HitboxPreview
{
    #region Constants

    public int GridViewSize => 144;

    public int AnchorBoxSize => 48;

    #endregion

    #region Registered Dependencies

    public static readonly DependencyProperty HitboxesProperty = DependencyProperty.Register(
        nameof(Hitboxes), typeof(ObservableCollection<Area>), typeof(HitboxPreview), new PropertyMetadata(null, OnBindingsChanged));

    public ObservableCollection<Area>? Hitboxes
    {
        get => (ObservableCollection<Area>?) GetValue(HitboxesProperty);
        set => SetValue(HitboxesProperty, value);
    }

    public static readonly DependencyProperty CharacterImageProperty = DependencyProperty.Register(
        nameof(CharacterImage), typeof(CroppedImage), typeof(HitboxPreview), new PropertyMetadata(null, OnBindingsChanged));

    public CroppedImage? CharacterImage
    {
        get => (CroppedImage?) GetValue(CharacterImageProperty);
        set => SetValue(CharacterImageProperty, value);
    }

    public static readonly DependencyProperty ShowAnchorProperty = DependencyProperty.Register(
        nameof(ShowAnchor), typeof(bool), typeof(HitboxPreview), new PropertyMetadata(false, OnBindingsChanged));

    public bool ShowAnchor
    {
        get => (bool) GetValue(ShowAnchorProperty);
        set => SetValue(ShowAnchorProperty, value);
    }

    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(HitboxPreview), new PropertyMetadata(false, OnBindingsChanged));

    public bool ShowGrid
    {
        get => (bool) GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public static readonly DependencyProperty PreviewsHitboxesProperty = DependencyProperty.Register(
        nameof(PreviewsHitboxes), typeof(ObservableCollection<Area>), typeof(HitboxPreview), new PropertyMetadata(null, OnBindingsChanged));

    public ObservableCollection<Area>? PreviewsHitboxes
    {
        get => (ObservableCollection<Area>?) GetValue(PreviewsHitboxesProperty);
        set => SetValue(PreviewsHitboxesProperty, value);
    }

    public static readonly DependencyProperty PreviewGridLinesProperty = DependencyProperty.Register(
        nameof(PreviewGridLines), typeof(ObservableCollection<GridLine>), typeof(HitboxPreview), new PropertyMetadata(null, OnBindingsChanged));

    public ObservableCollection<GridLine>? PreviewGridLines
    {
        get => (ObservableCollection<GridLine>?) GetValue(PreviewGridLinesProperty);
        set => SetValue(PreviewGridLinesProperty, value);
    }

    public static readonly DependencyProperty PreviewScaleProperty = DependencyProperty.Register(
        nameof(PreviewScale), typeof(double), typeof(HitboxPreview), new PropertyMetadata(1.0, OnBindingsChanged));

    public double PreviewScale
    {
        get => (double) GetValue(PreviewScaleProperty);
        set => SetValue(PreviewScaleProperty, value);
    }

    public static readonly DependencyProperty PreviewOffsetXProperty = DependencyProperty.Register(
        nameof(PreviewOffsetX), typeof(double), typeof(HitboxPreview), new PropertyMetadata(0.0, OnBindingsChanged));

    public double PreviewOffsetX
    {
        get => (double) GetValue(PreviewOffsetXProperty);
        set => SetValue(PreviewOffsetXProperty, value);
    }

    public static readonly DependencyProperty PreviewOffsetYProperty = DependencyProperty.Register(
        nameof(PreviewOffsetY), typeof(double), typeof(HitboxPreview), new PropertyMetadata(0.0, OnBindingsChanged));

    public double PreviewOffsetY
    {
        get => (double) GetValue(PreviewOffsetYProperty);
        set => SetValue(PreviewOffsetYProperty, value);
    }

    #endregion

    #region Constructor

    public HitboxPreview()
    {
        InitializeComponent();
    }

    #endregion

    #region Listeners

    private static void OnBindingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HitboxPreview control) control.Bindings.Update();
    }

    #endregion
}
