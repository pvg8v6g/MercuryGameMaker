using GameMaker.AppMain;
using GameMaker.UX.ViewModels.AttributesPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.AttributesPage;

public sealed partial class AttributesPage
{
    public AttributesPageViewModel ViewModel { get; }

    public AttributesPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<AttributesPageViewModel>();
        DataContext = ViewModel;
    }
}
