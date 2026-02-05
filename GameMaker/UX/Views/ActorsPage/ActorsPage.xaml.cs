using GameMaker.UX.ViewModels.ActorsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.ActorsPage;

public partial class ActorsPage
{
    public ActorsPageViewModel ViewModel { get; }

    public ActorsPage()
    {
        InitializeComponent();
        ViewModel = AppMain.App.Services!.GetRequiredService<ActorsPageViewModel>();
        DataContext = ViewModel;
    }
}
