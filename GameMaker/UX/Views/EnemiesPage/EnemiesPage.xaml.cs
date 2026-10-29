using GameMaker.AppMain;
using GameMaker.UX.ViewModels.EnemiesPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.EnemiesPage;

public partial class EnemiesPage
{
    public EnemiesPageViewModel ViewModel { get; }

    public EnemiesPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<EnemiesPageViewModel>();
        DataContext = ViewModel;
    }
}
