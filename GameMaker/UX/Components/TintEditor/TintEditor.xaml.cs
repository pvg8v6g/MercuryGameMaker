using GameLibrary.Utilities;
using MercuryLibrary.Enumerations;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GameMaker.UX.Components.TintEditor;

/// <summary>
/// Edits a tint: the color (saved as #RRGGBB), the strength from 0 to 1 and the blend mode (none lays the color flat).
/// Bind Color, Strength and BlendMode two way.
/// </summary>
public partial class TintEditor
{
    #region Constants

    private const string NoBlend = "None";

    #endregion

    #region Registered Dependencies

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color), typeof(string), typeof(TintEditor), new PropertyMetadata(null, OnColorChanged));

    public string? Color
    {
        get => (string?) GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register(
        nameof(Strength), typeof(double), typeof(TintEditor), new PropertyMetadata(0.0, OnStrengthChanged));

    public double Strength
    {
        get => (double) GetValue(StrengthProperty);
        set => SetValue(StrengthProperty, value);
    }

    public static readonly DependencyProperty BlendModeProperty = DependencyProperty.Register(
        nameof(BlendMode), typeof(object), typeof(TintEditor), new PropertyMetadata(null, OnBlendModeChanged));

    public BlendMode? BlendMode
    {
        get => (BlendMode?) GetValue(BlendModeProperty);
        set => SetValue(BlendModeProperty, value);
    }

    #endregion

    #region Fields

    // set while a property is being shown, so showing it doesn't write it back
    private bool _showing;

    #endregion

    #region Construction

    public TintEditor()
    {
        InitializeComponent();
        BlendBox.ItemsSource = new[] { NoBlend }.Concat(Enum.GetNames<BlendMode>()).ToArray();
        BlendBox.SelectedIndex = 0;

        Picker.ColorChanged += (_, e) =>
        {
            if (!_showing) Color = e.NewColor.ToHex();
        };
        StrengthSlider.ValueChanged += (_, e) =>
        {
            if (!_showing) Strength = e.NewValue;
        };
        BlendBox.SelectionChanged += (_, _) =>
        {
            if (_showing || BlendBox.SelectedItem is not string name) return;
            BlendMode = Enum.TryParse<BlendMode>(name, out var mode) ? mode : null;
        };
    }

    #endregion

    #region Private Methods

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TintEditor editor) return;

        var color = (e.NewValue as string).ToColorOr(Colors.White);
        if (editor.Picker.Color == color) return;
        editor.Show(() => editor.Picker.Color = color);
    }

    private static void OnStrengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TintEditor editor || Math.Abs(editor.StrengthSlider.Value - (double) e.NewValue) < 0.0001) return;
        editor.Show(() => editor.StrengthSlider.Value = (double) e.NewValue);
    }

    private static void OnBlendModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TintEditor editor) return;

        var name = e.NewValue is BlendMode mode ? mode.ToString() : NoBlend;
        if (editor.BlendBox.SelectedItem as string == name) return;
        editor.Show(() => editor.BlendBox.SelectedItem = name);
    }

    private void Show(Action show)
    {
        _showing = true;
        try
        {
            show();
        }
        finally
        {
            _showing = false;
        }
    }

    #endregion
}
