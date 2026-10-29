using System.Collections;
using System.Windows.Input;
using GameLibrary.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace GameMaker.UX.Components.EngineListView;

public sealed partial class EngineListView
{
    #region Registered Properties

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(object), typeof(EngineListView), new PropertyMetadata(null));

    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(EngineListView), new PropertyMetadata(-1));

    public int SelectedIndex
    {
        get => (int) GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(EngineListView), new PropertyMetadata(null, OnItemTemplateChanged));

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?) GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EngineListView engineListView)
        {
            engineListView.UpdateItemTemplate();
        }
    }

    private void UpdateItemTemplate()
    {
        ListView.ItemTemplate = ItemTemplate ?? (DataTemplate) ListView.Resources["DefaultItemTemplate"];
    }

    public static readonly DependencyProperty NewCommandProperty = DependencyProperty.Register(
        nameof(NewCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? NewCommand
    {
        get => (ICommand?) GetValue(NewCommandProperty);
        set => SetValue(NewCommandProperty, value);
    }

    public static readonly DependencyProperty NewCommandParameterProperty = DependencyProperty.Register(
        nameof(NewCommandParameter), typeof(object), typeof(EngineListView), new PropertyMetadata(null));

    public object? NewCommandParameter
    {
        get => GetValue(NewCommandParameterProperty);
        set => SetValue(NewCommandParameterProperty, value);
    }

    public static readonly DependencyProperty CopyCommandProperty = DependencyProperty.Register(
        nameof(CopyCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? CopyCommand
    {
        get => (ICommand?) GetValue(CopyCommandProperty);
        set => SetValue(CopyCommandProperty, value);
    }

    public static readonly DependencyProperty PasteCommandProperty = DependencyProperty.Register(
        nameof(PasteCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? PasteCommand
    {
        get => (ICommand?) GetValue(PasteCommandProperty);
        set => SetValue(PasteCommandProperty, value);
    }

    public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
        nameof(DeleteCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? DeleteCommand
    {
        get => (ICommand?) GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public static readonly DependencyProperty EditCommandProperty = DependencyProperty.Register(
        nameof(EditCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    public ICommand? EditCommand
    {
        get => (ICommand?) GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public static readonly DependencyProperty CopiedModelProperty = DependencyProperty.Register(
        nameof(CopiedModel), typeof(object), typeof(EngineListView), new PropertyMetadata(null));

    public object? CopiedModel
    {
        get => GetValue(CopiedModelProperty);
        set => SetValue(CopiedModelProperty, value);
    }

    public static readonly DependencyProperty NewOptionsProperty = DependencyProperty.Register(
        nameof(NewOptions), typeof(IEnumerable), typeof(EngineListView), new PropertyMetadata(null, OnNewOptionsChanged));

    /// <summary>
    /// When set, the single "New" entry is replaced by an "Add" submenu built from the supplied
    /// <see cref="EngineListNewOption"/> items, letting a list create several different item types.
    /// </summary>
    public IEnumerable? NewOptions
    {
        get => (IEnumerable?) GetValue(NewOptionsProperty);
        set => SetValue(NewOptionsProperty, value);
    }

    private static void OnNewOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EngineListView engineListView)
        {
            engineListView.UpdateNewOptions();
        }
    }

    private void UpdateNewOptions()
    {
        // Remove a previously built submenu (if any).
        var existing = ContextMenuFlyout.Items.FirstOrDefault(i => i is MenuFlyoutSubItem { Name: "AddSubMenu" });
        if (existing is not null)
        {
            ContextMenuFlyout.Items.Remove(existing);
        }

        if (NewOptions is null)
        {
            NewMenuItem.Visibility = Visibility.Visible;
            return;
        }

        // Replace the plain "New" item with a typed "Add" submenu.
        NewMenuItem.Visibility = Visibility.Collapsed;

        var subMenu = new MenuFlyoutSubItem
        {
            Name = "AddSubMenu",
            Text = "Add",
            Icon = new SymbolIcon(Symbol.Add)
        };

        foreach (var option in NewOptions.OfType<EngineListNewOption>())
        {
            subMenu.Items.Add(BuildOption(option));
        }

        ContextMenuFlyout.Items.Insert(0, subMenu);
    }

    private static MenuFlyoutItemBase BuildOption(EngineListNewOption option, Brush? inheritedForeground = null)
    {
        // Children inherit their parent group's accent colour unless they specify their own.
        var foreground = option.Foreground ?? inheritedForeground;

        if (option.Children is not null)
        {
            var group = new MenuFlyoutSubItem { Text = option.Text };
            if (foreground is not null)
            {
                group.Foreground = foreground;
            }

            foreach (var child in option.Children)
            {
                group.Items.Add(BuildOption(child, foreground));
            }

            return group;
        }

        var item = new MenuFlyoutItem
        {
            Text = option.Text,
            Command = option.Command,
            CommandParameter = option.CommandParameter
        };

        if (foreground is not null)
        {
            item.Foreground = foreground;
        }

        return item;
    }

    public static readonly DependencyProperty DoubleTapCommandProperty = DependencyProperty.Register(
        nameof(DoubleTapCommand), typeof(ICommand), typeof(EngineListView), new PropertyMetadata(null));

    /// <summary>
    /// When set, double-tapping a row executes this command (used to insert a new item above the tapped row)
    /// and then opens the context flyout so the item type can be chosen.
    /// </summary>
    public ICommand? DoubleTapCommand
    {
        get => (ICommand?) GetValue(DoubleTapCommandProperty);
        set => SetValue(DoubleTapCommandProperty, value);
    }

    #endregion

    public EngineListView()
    {
        InitializeComponent();
        UpdateItemTemplate();
        UpdateNewOptions();

        // Set these attached properties from code rather than XAML. In WinUI 3 the
        // PropertyChangedCallback for an attached property set on an element inside a
        // UserControl's XAML is not reliably invoked, so the right-tap/context handlers
        // were never wired up. Setting via SetValue here guarantees the callback fires.
        ListViewBehaviors.SetSelectOnRightClick(ListView, true);
        ListViewBehaviors.SetManageContextFlyoutSelection(ListView, true);
    }

    private void OnListViewDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (DoubleTapCommand is null) return;

        // Rows that opt out of the context menu (e.g. Else/End markers) ignore the
        // double-click "add action above" interaction as well.
        if ((e.OriginalSource as FrameworkElement)?.DataContext is IContextMenuSuppressible { SuppressContextMenu: true })
        {
            return;
        }

        if (DoubleTapCommand.CanExecute(null))
        {
            DoubleTapCommand.Execute(null);
        }

        ContextMenuFlyout.ShowAt(ListView, new FlyoutShowOptions { Position = e.GetPosition(ListView) });
    }
}
