using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Components.EngineGlyphButton;

/// <summary>
/// A button showing an icon from the Segoe Fluent icon font. Without a Title it is just the icon (its name and what it
/// does go in the tooltip). With a Title the caption sits small under the icon, like the map editor's tool rail.
/// It is a real Button, so Command, Flyout, Style and ToolTipService all work as usual.
/// </summary>
public partial class EngineGlyphButton : Button
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(EngineGlyphButton), new PropertyMetadata(string.Empty, OnLookChanged));

    public static readonly DependencyProperty GlyphSizeProperty = DependencyProperty.Register(
        nameof(GlyphSize), typeof(double), typeof(EngineGlyphButton), new PropertyMetadata(16.0, OnLookChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(EngineGlyphButton), new PropertyMetadata(null, OnLookChanged));

    private readonly FontIcon _icon = new();

    private readonly TextBlock _title = new() { FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center };

    private readonly StackPanel _titled = new() { Spacing = 2 };

    public EngineGlyphButton()
    {
        DefaultStyleKey = typeof(Button);
        UpdateLook();
    }

    // the icon's character, e.g. "&#xE805;" in XAML
    public string Glyph
    {
        get => (string) GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public double GlyphSize
    {
        get => (double) GetValue(GlyphSizeProperty);
        set => SetValue(GlyphSizeProperty, value);
    }

    // optional caption under the icon, none shows the icon alone
    public string? Title
    {
        get => (string?) GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private static void OnLookChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EngineGlyphButton button) button.UpdateLook();
    }

    private void UpdateLook()
    {
        _icon.Glyph = Glyph ?? string.Empty;
        _icon.FontSize = GlyphSize;

        // the icon lives in one parent at a time: alone, or above the caption
        _titled.Children.Clear();
        Content = null;
        if (string.IsNullOrEmpty(Title))
        {
            Content = _icon;
            return;
        }

        _title.Text = Title;
        _titled.Children.Add(_icon);
        _titled.Children.Add(_title);
        Content = _titled;
    }
}
