using GameEngine.AppMain;
using GameEngine.UX.ViewModels.MainGame;
using Microsoft.Extensions.DependencyInjection;

namespace GameEngine.UX.Views.MainGame;

public partial class MainGameView
{
    public MainGameViewModel ViewModel { get; }

    public MainGameView()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<MainGameViewModel>();
        DataContext = ViewModel;
    }
}
