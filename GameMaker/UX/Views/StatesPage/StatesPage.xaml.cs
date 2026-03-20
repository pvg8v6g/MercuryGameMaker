using GameMaker.AppMain;
using GameMaker.UX.ViewModels.StatesPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.StatesPage;

public partial class StatesPage
{
    public StatesPageViewModel ViewModel { get; }

    public StatesPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<StatesPageViewModel>();
        DataContext = ViewModel;
    }
}
