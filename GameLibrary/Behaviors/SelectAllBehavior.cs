using System.Reflection;
using Windows.Foundation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace GameLibrary.Behaviors;

public static class SelectAllBehavior
{
    public static readonly DependencyProperty EnableTrailingDoubleClickSelectAllProperty =
        DependencyProperty.RegisterAttached(
            "EnableTrailingDoubleClickSelectAll",
            typeof(bool),
            typeof(SelectAllBehavior),
            new PropertyMetadata(false, OnEnableTrailingDoubleClickSelectAllChanged));

    public static void SetEnableTrailingDoubleClickSelectAll(DependencyObject element, bool value)
    {
        element.SetValue(EnableTrailingDoubleClickSelectAllProperty, value);
    }

    public static bool GetEnableTrailingDoubleClickSelectAll(DependencyObject element)
    {
        return (bool) element.GetValue(EnableTrailingDoubleClickSelectAllProperty);
    }

    private static void OnEnableTrailingDoubleClickSelectAllChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox tb) return;
        var state = GetOrCreateState(tb);
        var enabled = (bool) e.NewValue;
        if (enabled)
        {
            state.OnLoadedHandler ??= OnTextBoxLoaded;
            tb.Loaded += state.OnLoadedHandler;
            TryAttach(tb, state);
        }
        else
        {
            Detach(state);
            if (state.OnLoadedHandler is not null)
            {
                tb.Loaded -= state.OnLoadedHandler;
                state.OnLoadedHandler = null;
            }

            States.Remove(tb);
        }
    }

    private static void TryAttach(TextBox tb, State state)
    {
        try
        {
            var getTemplateChild = tb.GetType().GetMethod(
                "GetTemplateChild",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(string)],
                null);
            var svObj = getTemplateChild?.Invoke(tb, ["ContentElement"]);
            if (svObj is not ScrollViewer sv) return;
            sv.PointerPressed += OnPointerPressed;
            state.ScrollViewer = sv;
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static void Detach(State state)
    {
        if (state.ScrollViewer is null) return;
        state.ScrollViewer.PointerPressed -= OnPointerPressed;
        state.ScrollViewer = null;
    }

    private static void OnTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb || !States.TryGetValue(tb, out var state) || state.OnLoadedHandler is null) return;
        tb.Loaded -= state.OnLoadedHandler;
        state.OnLoadedHandler = null;
        TryAttach(tb, state);
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var tb = sender as TextBox;
        if (tb is null && sender is ScrollViewer viewer) tb = FindAncestor<TextBox>(viewer);
        if (tb is null || !e.GetCurrentPoint(tb).Properties.IsLeftButtonPressed) return;
        var pointerPoint = e.GetCurrentPoint(tb);
        var point = pointerPoint.Position;
        var state = GetOrCreateState(tb);
        var now = DateTime.UtcNow;
        var isDoubleClick = (now - state.LastClickTime).TotalMilliseconds < 400 &&
                            Math.Abs(point.X - state.LastPosition.X) < 10 &&
                            Math.Abs(point.Y - state.LastPosition.Y) < 10;
        state.LastClickTime = now;
        state.LastPosition = point;
        if (isDoubleClick)
        {
            tb.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                if (tb.SelectionLength <= 1 && tb.SelectionStart == tb.Text.Length)
                {
                    tb.SelectAll();
                }
            });
        }
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(current);
        while (parent != null)
        {
            if (parent is T t) return t;
            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private static State GetOrCreateState(TextBox tb)
    {
        if (States.TryGetValue(tb, out var state)) return state;
        state = new State();
        States[tb] = state;
        return state;
    }

    private static readonly Dictionary<TextBox, State> States = new();

    private class State
    {
        public DateTime LastClickTime = DateTime.MinValue;
        public Point LastPosition;
        public ScrollViewer? ScrollViewer;
        public RoutedEventHandler? OnLoadedHandler;
    }
}
