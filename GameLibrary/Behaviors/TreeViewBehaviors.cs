using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace GameLibrary.Behaviors;

public static class TreeViewBehaviors
{
    #region SelectedItem

    private static readonly object UnsetSentinel = new();

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.RegisterAttached(
        "SelectedItem", typeof(object), typeof(TreeViewBehaviors), new PropertyMetadata(UnsetSentinel, OnSelectedItemChanged));

    public static object GetSelectedItem(DependencyObject obj)
    {
        return obj.GetValue(SelectedItemProperty);
    }

    public static void SetSelectedItem(DependencyObject obj, object value)
    {
        obj.SetValue(SelectedItemProperty, value);
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView treeView) return;

        treeView.SelectionChanged -= TreeView_SelectionChanged;
        treeView.SelectionChanged += TreeView_SelectionChanged;
    }

    private static void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        var selected = args.AddedItems.Count > 0 ? args.AddedItems[0] : null;
        if (!ReferenceEquals(GetSelectedItem(sender), selected))
        {
            SetSelectedItem(sender, selected);
        }
    }

    #endregion

    #region DoubleTapCommand

    public static readonly DependencyProperty DoubleTapCommandProperty = DependencyProperty.RegisterAttached(
        "DoubleTapCommand", typeof(ICommand), typeof(TreeViewBehaviors), new PropertyMetadata(null, OnDoubleTapCommandChanged));

    public static ICommand GetDoubleTapCommand(DependencyObject obj)
    {
        return (ICommand) obj.GetValue(DoubleTapCommandProperty);
    }

    public static void SetDoubleTapCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(DoubleTapCommandProperty, value);
    }

    private static void OnDoubleTapCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TreeView treeView) return;

        treeView.DoubleTapped -= TreeView_DoubleTapped;
        treeView.DoubleTapped += TreeView_DoubleTapped;
    }

    private static void TreeView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not TreeView treeView) return;

        var command = GetDoubleTapCommand(treeView);
        var parameter = treeView.SelectedItem;
        if (command is not null && parameter is not null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    #endregion
}
