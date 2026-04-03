using System.Collections.ObjectModel;
using GameLibrary.Models.Areas;
using GameLibrary.Utilities.ComponentModels;
using GameMaker.AppMain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.GridArea;

public partial class AreaCreatorView
{
    #region Registered Dependencies

    public static readonly DependencyProperty HitboxesProperty = DependencyProperty.Register(
        nameof(Hitboxes), typeof(ObservableCollection<Area>), typeof(AreaCreatorView), new PropertyMetadata(null, OnHitboxesChanged));

    public ObservableCollection<Area>? Hitboxes
    {
        get => (ObservableCollection<Area>?) GetValue(HitboxesProperty);
        set => SetValue(HitboxesProperty, value);
    }

    public static readonly DependencyProperty CharacterImageProperty = DependencyProperty.Register(nameof(CharacterImage), typeof(CroppedImage),
        typeof(AreaCreatorView), new PropertyMetadata(null, OnCharacterImageChanged));

    public CroppedImage? CharacterImage
    {
        get => (CroppedImage?) GetValue(CharacterImageProperty);
        set => SetValue(CharacterImageProperty, value);
    }

    #endregion

    #region Constructor

    private AreaCreatorViewModel ViewModel { get; }

    public AreaCreatorView()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<AreaCreatorViewModel>();
        DataContext = ViewModel;
    }

    #endregion

    #region Listeners

    private static void OnHitboxesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AreaCreatorView control) return;
        var hitboxes = e.NewValue as ObservableCollection<Area>;
        control.ViewModel.OnHitboxesChanged(hitboxes ?? []);
    }

    private static void OnCharacterImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AreaCreatorView control) return;
        control.ViewModel.OnCharacterImageChanged(e.NewValue as CroppedImage);
    }

    #endregion
}
