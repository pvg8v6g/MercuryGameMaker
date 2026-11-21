using System.ComponentModel;
using GameLibrary.Utilities;
using GameLibrary.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace GameMaker.UX.Views.Popups.Progress;

public partial class ProgressPopup
{
    #region Properties

    public EngineTask EngineTask { get; set; } = null!;

    public string ProgressText
    {
        get => ProgressLabel.Text;
        set => ProgressLabel.Text = value;
    }

    #endregion

    public ProgressPopup()
    {
        InitializeComponent();
    }

    private void ValueOnChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (e.NewValue < 100) return;
        Hide();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Run().Forget();
    }

    private async Task Run()
    {
        if (!EngineTask.RunsInBackground)
        {
            // work that changes what's on screen stays on the UI thread
            var progressBinding = new Binding
            {
                Source = EngineTask,
                Path = new PropertyPath("Progress"),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };
            BindingOperations.SetBinding(ProgressBar, RangeBase.ValueProperty, progressBinding);
            await EngineTask.Call();
            return;
        }

        // background work: progress comes in on the worker thread and is handed to the UI thread to draw
        var queue = DispatcherQueue;
        EngineTask.PropertyChanged += OnProgress;
        try
        {
            await Task.Run(EngineTask.Call);
        }
        finally
        {
            EngineTask.PropertyChanged -= OnProgress;
            // finished, or failed (the error is logged by Forget): either way the popup closes instead of hanging
            ProgressBar.Value = 100;
            Hide();
        }

        void OnProgress(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(EngineTask.Progress)) return;
            var progress = EngineTask.Progress;
            queue.TryEnqueue(() => ProgressBar.Value = Math.Min(progress, 99)); // 100 closes it, which the finally does
        }
    }
}
