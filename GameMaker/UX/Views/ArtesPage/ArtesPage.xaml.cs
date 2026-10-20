using GameMaker.AppMain;
using GameMaker.UX.ViewModels.ArtesPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.ArtesPage;

public partial class ArtesPage
{
    public ArtesPageViewModel ViewModel { get; }

    public ArtesPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<ArtesPageViewModel>();
        DataContext = ViewModel;
    }
}
