using GameMaker.AppMain;
using GameMaker.UX.ViewModels.MaterialsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.MaterialsPage;

public partial class MaterialsPage
{
    public MaterialsPageViewModel ViewModel { get; }

    public MaterialsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<MaterialsPageViewModel>();
        DataContext = ViewModel;
    }
}
