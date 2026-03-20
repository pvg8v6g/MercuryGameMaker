using System.Collections.ObjectModel;
using GameLibrary.Models.Growths;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.DisciplinesPage;

public partial class AttributeGrowthSetting : PropertyChangedUpdater
{
    public ObservableCollection<Growth> AvailableGrowths { get; set; } = [];

    public int IconIndex { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid AttributeGuid { get; set; }

    public Guid? GrowthGuid
    {
        get;
        set => SetField(ref field, value);
    }

    public ISeries[] Series
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public Axis[] XAxes
    {
        get;
        set => SetField(ref field, value);
    } = [];

    public Axis[] YAxes
    {
        get;
        set => SetField(ref field, value);
    } = [];
}
