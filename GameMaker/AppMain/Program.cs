using GameLibrary.Startup;

namespace GameMaker.AppMain;

// the startup WinUI would generate, with a safety check first (DISABLE_XAML_GENERATED_MAIN in the project turns theirs off)
public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // a long install path crashes WinUI at random before any of our code runs, see PriPathGuard
        if (!PriPathGuard.EnsureSafe(args)) return;

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(_ =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
