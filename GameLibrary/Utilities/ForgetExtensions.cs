using System.Diagnostics;

namespace GameLibrary.Utilities;

/// <summary>
/// For the places that have to return void (WinUI overrides, event handlers, property changed callbacks, ICommand.Execute):
/// task.Forget() runs the task without async void, so a failure is caught and reported instead of killing the process.
/// </summary>
public static class ForgetExtensions
{
    #region Events

    // each app listens to this once (the engine writes crash.log, the editor its own log)
    public static event Action<Exception>? Failed;

    #endregion

    #region Methods

    public static void Forget(this Task task)
    {
        _ = Observe(task);
    }

    private static async Task Observe(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // a cancelled task (a fade, a load that was replaced) is not a failure
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            Failed?.Invoke(exception);
        }
    }

    #endregion
}
