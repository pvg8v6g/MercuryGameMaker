using GameMaker.AppMain;
using GameMaker.UX.ViewModels.TermsPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.TermsPage;

public partial class TermsPage
{
    public TermsPageViewModel ViewModel { get; }

    public TermsPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<TermsPageViewModel>();
        DataContext = ViewModel;
    }
}
