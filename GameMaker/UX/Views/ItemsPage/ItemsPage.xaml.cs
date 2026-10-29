using GameMaker.AppMain;
using GameMaker.UX.ViewModels.ItemsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.ItemsPage;

public partial class ItemsPage
{
    public ItemsPageViewModel ViewModel { get; }

    public ItemsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<ItemsPageViewModel>();
        DataContext = ViewModel;
    }
}
