using GameMaker.AppMain;
using GameMaker.UX.ViewModels.MainPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.MainPage;

public partial class MainPage
{
    public MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<MainPageViewModel>();
        DataContext = ViewModel;
    }
}
