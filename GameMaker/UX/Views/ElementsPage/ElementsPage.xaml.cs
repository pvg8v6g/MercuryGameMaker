using GameMaker.AppMain;
using GameMaker.UX.ViewModels.ElementsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.ElementsPage;

public partial class ElementsPage
{
    public ElementsPageViewModel ViewModel { get; }

    public ElementsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<ElementsPageViewModel>();
        DataContext = ViewModel;
    }
}
