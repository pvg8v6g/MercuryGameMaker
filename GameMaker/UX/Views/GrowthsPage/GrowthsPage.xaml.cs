using GameMaker.AppMain;
using GameMaker.UX.ViewModels.GrowthsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.GrowthsPage;

public partial class GrowthsPage
{
    public GrowthsPageViewModel ViewModel { get; }

    public GrowthsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<GrowthsPageViewModel>();
        DataContext = ViewModel;
    }
}
