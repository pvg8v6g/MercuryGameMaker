using GameLibrary.Models;
using Microsoft.UI.Xaml;

namespace GameMaker.UX.Components.EngineListView.Items;

public sealed partial class EngineIdNameItem
{
    public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
        nameof(Model), typeof(BaseModel), typeof(EngineIdNameItem), new PropertyMetadata(null));

    public BaseModel? Model
    {
        get => (BaseModel?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public EngineIdNameItem()
    {
        InitializeComponent();
    }
}
