using GameMaker.AppMain;
using GameMaker.UX.ViewModels.DisciplinesPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.DisciplinesPage;

public partial class DisciplinesPage
{
    public DisciplinesPageViewModel ViewModel { get; }

    public DisciplinesPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<DisciplinesPageViewModel>();
        DataContext = ViewModel;
    }
}
