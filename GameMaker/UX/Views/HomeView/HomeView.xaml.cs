using GameMaker.UX.ViewModels.HomeView;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.HomeView;

public partial class HomeView
{
    public HomeViewViewModel ViewModel { get; }

    public HomeView()
    {
        InitializeComponent();
        ViewModel = AppMain.App.Services!.GetRequiredService<HomeViewViewModel>();
        ViewModel.NavigationService.TopFrame = TopBarFrame;
        ViewModel.NavigationService.ActiveFrame = ActiveFrame;
        DataContext = ViewModel;
    }
}
