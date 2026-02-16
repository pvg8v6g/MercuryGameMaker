using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
        }
        else
        {
            listView.RightTapped -= ListView_RightTapped;
        }
    }

    private static void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is not ListView listView) return;

        var frameworkElement = e.OriginalSource as FrameworkElement;
        var dataContext = frameworkElement?.DataContext;

        if (dataContext == null) return;

        listView.SelectedItem = dataContext;
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
            listView.RegisterPropertyChangedCallback(UIElement.ContextFlyoutProperty, (s, p) => HookFlyout(listView));
            HookFlyout(listView);
        }
    }

    private static readonly ConditionalWeakTable<MenuFlyout, ListView> FlyoutToListViewMap = new();

    private static void HookFlyout(ListView listView)
    {
        if (listView.ContextFlyout is MenuFlyout flyout)
        {
            flyout.Opening -= Flyout_Opening;
            flyout.Opening += Flyout_Opening;

            FlyoutToListViewMap.Remove(flyout);
            FlyoutToListViewMap.Add(flyout, listView);
        }
    }

    private static void Flyout_Opening(object sender, object e)
    {
        if (sender is MenuFlyout flyout)
        {
            if (!FlyoutToListViewMap.TryGetValue(flyout, out var listView))
            {
                listView = flyout.Target as ListView;
            }

            if (listView != null)
            {
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
