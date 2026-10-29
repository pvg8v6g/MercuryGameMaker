using GameMaker.AppMain;
using GameMaker.UX.ViewModels.TroopsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.TroopsPage;

public partial class TroopsPage
{
    public TroopsPageViewModel ViewModel { get; }

    public TroopsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<TroopsPageViewModel>();
        DataContext = ViewModel;
    }
}
