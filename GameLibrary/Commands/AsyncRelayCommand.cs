using GameLibrary.Utilities;
using System.Windows.Input;

namespace GameLibrary.Commands;

public partial class AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    #region ICommand Members

    public bool CanExecute(object? parameter)
    {
        return canExecute == null || canExecute();
    }

    // ICommand.Execute has to return void, Forget runs the task and reports a failure instead of crashing
    public void Execute(object? parameter)
    {
        execute().Forget();
    }

    #endregion

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public partial class AsyncRelayCommand<T>(Func<T, Task> execute, Func<T, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (parameter is null) return false;
        return canExecute == null || canExecute((T) parameter);
    }

    public void Execute(object? parameter)
    {
        if (parameter is null) return;
        execute((T) parameter).Forget();
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
