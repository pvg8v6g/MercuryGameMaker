using GameMaker.UX.Models.Weathers;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.WeatherEditor;

/// <summary>
/// Edits a weather: its kind, how much falls and how, the sky, its color and the sound that loops with it.
/// Bind Model to the weather's editor model.
/// </summary>
public partial class WeatherEditor
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(WeatherEditorModel), typeof(WeatherEditor), new PropertyMetadata(null));

    public WeatherEditorModel? Model
    {
        get => (WeatherEditorModel?) GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public WeatherEditor()
    {
        InitializeComponent();
    }
}
