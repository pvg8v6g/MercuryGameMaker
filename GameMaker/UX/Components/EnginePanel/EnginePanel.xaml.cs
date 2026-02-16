using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace GameMaker.UX.Components.EnginePanel;

[ContentProperty(Name = nameof(InnerContent))]
public partial class EnginePanel
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(EnginePanel), new PropertyMetadata(null));

    public string Title
    {
        get => (string) GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register(
        nameof(InnerContent), typeof(object), typeof(EnginePanel), new PropertyMetadata(null));

    public object InnerContent
    {
        get => GetValue(InnerContentProperty);
        set => SetValue(InnerContentProperty, value);
    }

    public EnginePanel()
    {
        InitializeComponent();
    }
}
