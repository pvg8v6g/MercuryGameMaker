using GameMaker.AppMain;
using GameMaker.UX.ViewModels.MediaPlayerPage;
using Microsoft.Extensions.DependencyInjection;

namespace GameMaker.UX.Views.MediaPlayerPage;

public partial class MediaPlayerPage
{
    public MediaPlayerPageViewModel ViewModel { get; }

    public MediaPlayerPage()
    {
        InitializeComponent();
        ViewModel = App.Services!.GetRequiredService<MediaPlayerPageViewModel>();
        DataContext = ViewModel;
    }
}
