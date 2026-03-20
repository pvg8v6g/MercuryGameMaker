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
        if (d is TextBox tb)
        {
            bool enabled = (bool) e.NewValue;
            if (enabled)
            {
                Console.WriteLine("Attached");
                tb.PointerPressed += OnPointerPressed;
            }
            else
            {
                tb.PointerPressed -= OnPointerPressed;
                _states.Remove(tb);
            }
        }
    }

    private static bool TryAttach(TextBox tb, State state)
    {
        var svObj = tb.GetType().GetMethod("GetTemplateChild", new[] { typeof(string) })?.Invoke(tb, new object[] { "ContentElement" });
        var sv = svObj as ScrollViewer;
        if (sv == null) return false;
        sv.PointerPressed += OnPointerPressed;
        state.ScrollViewer = sv;
        return true;
    }

    private static void Detach(TextBox tb, State state)
    {
        if (state.ScrollViewer != null)
        {
            state.ScrollViewer.PointerPressed -= OnPointerPressed;
            state.ScrollViewer = null;
        }
    }

    private static void OnTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && _states.TryGetValue(tb, out State? state) && state.OnLoadedHandler != null)
        {
            Console.WriteLine("Loaded");
            tb.Loaded -= state.OnLoadedHandler;
            state.OnLoadedHandler = null;
            TryAttach(tb, state);
        }
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Console.WriteLine("Pointer Pressed");
        TextBox? tb = sender as TextBox;
        if (tb == null && sender is ScrollViewer)
        {
            tb = FindAncestor<TextBox>(sender as DependencyObject);
        }

        if (tb == null || !e.GetCurrentPoint(tb).Properties.IsLeftButtonPressed) return;
        var pointerPoint = e.GetCurrentPoint(tb);
        var point = pointerPoint.Position;
        var state = GetOrCreateState(tb);
        var now = DateTime.UtcNow;
        bool isDoubleClick = (now - state.LastClickTime).TotalMilliseconds < 400 &&
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
            current = parent;
            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private static State GetOrCreateState(TextBox tb)
    {
        if (!_states.TryGetValue(tb, out State? state))
        {
            state = new State();
            _states[tb] = state;
        }

        return state;
    }

    private static readonly Dictionary<TextBox, State> _states = new();

    private class State
    {
        public DateTime LastClickTime = DateTime.MinValue;
        public Point LastPosition;
        public ScrollViewer? ScrollViewer;
        public RoutedEventHandler? OnLoadedHandler;
    }
}
