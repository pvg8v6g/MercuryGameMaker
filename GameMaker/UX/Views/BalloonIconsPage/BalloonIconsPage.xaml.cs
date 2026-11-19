using GameMaker.AppMain;
using GameMaker.UX.ViewModels.BalloonIconsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.BalloonIconsPage;

public partial class BalloonIconsPage
{
    public BalloonIconsPageViewModel ViewModel { get; }

    public BalloonIconsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<BalloonIconsPageViewModel>();
        DataContext = ViewModel;
    }
}
