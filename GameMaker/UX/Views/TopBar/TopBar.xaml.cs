using GameMaker.UX.ViewModels.TopBar;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Views.TopBar;

public partial class TopBar : Page
{
    public TopBarViewModel ViewModel { get; }

    public TopBar()
    {
        InitializeComponent();
        ViewModel = AppMain.App.Services!.GetRequiredService<TopBarViewModel>();
        DataContext = ViewModel;
    }
}
