using GameMaker.UX.ViewModels.TopBar;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.TopBar;

public partial class TopBar
{
    public TopBarViewModel ViewModel { get; }

    public TopBar()
    {
        ViewModel = AppMain.App.Services!.GetRequiredService<TopBarViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }
}
