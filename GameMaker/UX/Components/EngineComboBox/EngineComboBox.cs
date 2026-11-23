using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using GameLibrary.Behaviors;

namespace GameMaker.UX.Components.EngineComboBox;

/// <summary>
/// A ComboBox that can optionally show a clear "X" inside the box, at the right where the caret is. While the X shows
/// the caret is hidden; with nothing picked, or with IsClearable off (the default), it looks like a normal ComboBox.
/// </summary>
/// <remarks>
/// The X is added to WinUI's own ComboBox template when it's applied rather than by copying that template, because the
/// template uses WinUI's internal resources (ComboBoxPadding and the like) that the app's own XAML can't look up.
/// </remarks>
public sealed partial class EngineComboBox : ComboBox
{
    #region Constants

    // the X's size, and its right margin, which puts its icon about where the caret's is
    private const double ClearButtonSize = 24;
    private const double ClearButtonRight = 6;

    #endregion

    #region Dependency Properties

    public static readonly DependencyProperty IsClearableProperty = DependencyProperty.Register(
        nameof(IsClearable), typeof(bool), typeof(EngineComboBox), new PropertyMetadata(false, OnIsClearableChanged));

    public bool IsClearable
    {
        get => (bool) GetValue(IsClearableProperty);
        set => SetValue(IsClearableProperty, value);
    }

    #endregion

    #region Fields

    private Button? _clearButton;
    private FrameworkElement? _caret;

    #endregion

    #region Constructor

    public EngineComboBox()
    {
        SelectionChanged += OnSelectionChanged;
    }

    #endregion

    #region Overrides

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_clearButton is not null) _clearButton.Click -= OnClearClick;
        _clearButton = null;

        _caret = GetTemplateChild("DropDownGlyph") as FrameworkElement;
        if (GetTemplateChild("LayoutRoot") is Grid root) _clearButton = AddClearButton(root);

        UpdateClearButton();
    }

    #endregion

    #region Listeners

    private static void OnIsClearableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((EngineComboBox) d).UpdateClearButton();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateClearButton();

    // picking nothing also sets SelectedItem and SelectedValue to null, so whatever they're bound to is cleared too
    private void OnClearClick(object sender, RoutedEventArgs e) => SelectedIndex = -1;

    #endregion

    #region Private Methods

    // the X goes in the caret's column on the box's row, against the right edge
    private Button AddClearButton(Grid root)
    {
        // a FontIcon already uses the symbol font
        var icon = new FontIcon
        {
            FontSize = 10,
            Glyph = ""
        };

        // takes the caret's colour, so it follows the box's hover, focus and disabled looks
        if (_caret is not null)
        {
            icon.SetBinding(IconElement.ForegroundProperty, new Binding { Source = _caret, Path = new PropertyPath("Foreground") });
        }

        var button = new Button
        {
            Content = icon,
            Width = ClearButtonSize,
            Height = ClearButtonSize,
            MinWidth = 0,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, ClearButtonRight, 0),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            IsTabStop = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        ToolTipService.SetToolTip(button, "Clear");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, "Clear");
        PointerCursorBehavior.SetCursorType(button, InputSystemCursorShape.Hand);
        Grid.SetRow(button, 1);
        Grid.SetColumn(button, 1);

        button.Click += OnClearClick;
        root.Children.Add(button);
        return button;
    }

    // the X and the caret take turns in the same spot
    private void UpdateClearButton()
    {
        if (_clearButton is null) return;

        var shows = IsClearable && SelectedItem is not null;
        _clearButton.Visibility = shows ? Visibility.Visible : Visibility.Collapsed;
        if (_caret is not null) _caret.Visibility = shows ? Visibility.Collapsed : Visibility.Visible;
    }

    #endregion
}
