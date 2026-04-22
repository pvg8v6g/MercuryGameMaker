using GameMaker.AppMain;
using GameMaker.UX.ViewModels.AnimationsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.AnimationsPage;

public partial class AnimationsPage
{
    public AnimationsPageViewModel ViewModel { get; }

    public AnimationsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<AnimationsPageViewModel>();
        DataContext = ViewModel;
    }
}
