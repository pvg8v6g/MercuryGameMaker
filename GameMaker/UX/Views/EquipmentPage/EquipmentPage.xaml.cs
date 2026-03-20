using GameMaker.AppMain;
using GameMaker.UX.ViewModels.EquipmentPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.EquipmentPage;

public partial class EquipmentPage
{
    public EquipmentPageViewModel ViewModel { get; }

    public EquipmentPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<EquipmentPageViewModel>();
        DataContext = ViewModel;
    }
}
