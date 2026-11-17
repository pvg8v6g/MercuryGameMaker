using GameMaker.AppMain;
using GameMaker.UX.ViewModels.PropsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.PropsPage;

public partial class PropsPage
{
    public PropsPageViewModel ViewModel { get; }

    public PropsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<PropsPageViewModel>();
        DataContext = ViewModel;
    }
}
