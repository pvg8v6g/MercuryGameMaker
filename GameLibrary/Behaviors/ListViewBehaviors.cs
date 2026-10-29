using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace GameLibrary.Behaviors;

public static class ListViewBehaviors
{
    public static readonly DependencyProperty SelectOnRightClickProperty = DependencyProperty.RegisterAttached(
        "SelectOnRightClick", typeof(bool), typeof(ListViewBehaviors), new PropertyMetadata(false, OnSelectOnRightClickChanged));

    public static bool GetSelectOnRightClick(DependencyObject obj)
    {
        return (bool) obj.GetValue(SelectOnRightClickProperty);
    }

    public static void SetSelectOnRightClick(DependencyObject obj, bool value)
    {
        obj.SetValue(SelectOnRightClickProperty, value);
    }

    private static void OnSelectOnRightClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView listView) return;

        if ((bool) e.NewValue)
        {
            listView.RightTapped += ListView_RightTapped;
            listView.ContextRequested += ListView_ContextRequested;
        }
        else
        {
            listView.RightTapped -= ListView_RightTapped;
            listView.ContextRequested -= ListView_ContextRequested;
        }
    }

    private static void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not ListView listView) return;

        var frameworkElement = e.OriginalSource as FrameworkElement;
        var dataContext = frameworkElement?.DataContext;

        if (dataContext == null) return;

        // Rows that opt out of the context menu (e.g. Else/End markers) swallow the
        // right-tap so the default context flyout is never shown for them. The framework
        // opens ListView.ContextFlyout automatically on the ContextRequested event (which
        // fires right after RightTapped), and setting e.Handled is not reliable enough to
        // stop it. Instead we temporarily detach the ContextFlyout so there is nothing for
        // the framework to open, then restore it on the next dispatcher tick.
        if (dataContext is IContextMenuSuppressible { SuppressContextMenu: true })
        {
            e.Handled = true;

            if (listView.ContextFlyout is { } flyoutToSuppress)
            {
                listView.ContextFlyout = null;
                var queue = listView.DispatcherQueue;
                if (queue != null)
                {
                    queue.TryEnqueue(() => listView.ContextFlyout ??= flyoutToSuppress);
                }
                else
                {
                    listView.ContextFlyout = flyoutToSuppress;
                }
            }

            return;
        }

        listView.SelectedItem = dataContext;

        // Right-tapping a row and the context flyout's Opening event do not have a
        // guaranteed order. On pages where the flyout's deferred enable/disable update
        // runs before this selection is committed, every selection-requiring item would
        // stay greyed out until an empty-space right-click. Since we just selected a valid
        // item here, refresh the menu items synchronously so enablement no longer depends
        // on event ordering.
        if (listView.ContextFlyout is MenuFlyout flyout)
        {
            UpdateMenuItems(flyout.Items, listView);
        }
    }

    private static void ListView_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
    {
        // For non-pointer triggers (e.g. the keyboard menu key) RightTapped does not fire,
        // so suppress here based on the resolved row data context.
        var dataContext = ResolveRowDataContext(e.OriginalSource as DependencyObject);
        if (dataContext is IContextMenuSuppressible { SuppressContextMenu: true })
        {
            e.Handled = true;
        }
    }

    private static object? ResolveRowDataContext(DependencyObject? source)
    {
        var current = source;
        while (current != null)
        {
            switch (current)
            {
                case ListViewItem item:
                    return item.Content ?? item.DataContext;
                case FrameworkElement { DataContext: IContextMenuSuppressible } fe:
                    return fe.DataContext;
                default:
                    current = VisualTreeHelper.GetParent(current);
                    break;
            }
        }

        // Fall back to the originating element's own data context.
        return (source as FrameworkElement)?.DataContext;
    }

    public static readonly DependencyProperty RequireSelectionForMenuFlyoutItemProperty = DependencyProperty.RegisterAttached(
        "RequireSelectionForMenuFlyoutItem", typeof(bool), typeof(ListViewBehaviors), new PropertyMetadata(false));

    public static bool GetRequireSelectionForMenuFlyoutItem(DependencyObject obj)
    {
        return (bool) obj.GetValue(RequireSelectionForMenuFlyoutItemProperty);
    }

    public static void SetRequireSelectionForMenuFlyoutItem(DependencyObject obj, bool value)
    {
        obj.SetValue(RequireSelectionForMenuFlyoutItemProperty, value);
    }

    public static readonly DependencyProperty ManageContextFlyoutSelectionProperty = DependencyProperty.RegisterAttached(
        "ManageContextFlyoutSelection", typeof(bool), typeof(ListViewBehaviors), new PropertyMetadata(false, OnManageContextFlyoutSelectionChanged));

    public static bool GetManageContextFlyoutSelection(DependencyObject obj)
    {
        return (bool) obj.GetValue(ManageContextFlyoutSelectionProperty);
    }

    public static void SetManageContextFlyoutSelection(DependencyObject obj, bool value)
    {
        obj.SetValue(ManageContextFlyoutSelectionProperty, value);
    }

    private static void OnManageContextFlyoutSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView listView) return;

        if ((bool) e.NewValue)
        {
            listView.RegisterPropertyChangedCallback(UIElement.ContextFlyoutProperty, (_, _) => HookFlyout(listView));
            HookFlyout(listView);
        }
    }

    private static readonly ConditionalWeakTable<MenuFlyout, ListView> FlyoutToListViewMap = new();

    private static void HookFlyout(ListView listView)
    {
        if (listView.ContextFlyout is not MenuFlyout flyout) return;
        flyout.Opening -= Flyout_Opening;
        flyout.Opening += Flyout_Opening;

        FlyoutToListViewMap.Remove(flyout);
        FlyoutToListViewMap.Add(flyout, listView);
    }

    private static void Flyout_Opening(object? sender, object e)
    {
        if (sender is not MenuFlyout flyout) return;

        if (!FlyoutToListViewMap.TryGetValue(flyout, out var listView))
        {
            listView = flyout.Target as ListView;
        }

        if (listView == null) return;
        // We use DispatcherQueue to ensure that any selection changes from RightTapped
        // are processed before we update the menu items' IsEnabled state.
        var dispatcherQueue = flyout.DispatcherQueue ?? listView.DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(() => { UpdateMenuItems(flyout.Items, listView); });
        }
        else
        {
            UpdateMenuItems(flyout.Items, listView);
        }
    }

    private static void UpdateMenuItems(IEnumerable<MenuFlyoutItemBase> items, ListView listView)
    {
        foreach (var itemBase in items)
        {
            if (itemBase is MenuFlyoutItem item)
            {
                if (GetRequireSelectionForMenuFlyoutItem(item))
                {
                    item.IsEnabled = listView.SelectedIndex >= 0;
                }
            }
            else if (itemBase is MenuFlyoutSubItem subItem)
            {
                UpdateMenuItems(subItem.Items, listView);
            }
        }
    }
}
