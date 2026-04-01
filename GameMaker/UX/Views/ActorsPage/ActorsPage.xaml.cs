using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using GameMaker.UX.ViewModels.ActorsPage;

namespace GameMaker.UX.Views.ActorsPage;

public partial class ActorsPage
{
    public ActorsPageViewModel ViewModel { get; }

    public ActorsPage()
    {
        InitializeComponent();
        ViewModel = AppMain.App.Services!.GetRequiredService<ActorsPageViewModel>();
        DataContext = ViewModel;
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        FlyoutBase.ShowAttachedFlyout(RootButton);
    }
}
