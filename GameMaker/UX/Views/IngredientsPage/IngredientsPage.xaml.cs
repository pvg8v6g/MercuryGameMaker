using GameMaker.AppMain;
using GameMaker.UX.ViewModels.IngredientsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.IngredientsPage;

public partial class IngredientsPage
{
    public IngredientsPageViewModel ViewModel { get; }

    public IngredientsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<IngredientsPageViewModel>();
        DataContext = ViewModel;
    }
}
