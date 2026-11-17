using GameLibrary.Commands;
using GameLibrary.Enumerations;
using GameLibrary.Models.Props;
using GameMaker.UX.Models.MainPage;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using FillRule = GameLibrary.Models.Props.FillRule;

namespace GameMaker.UX.ViewModels.Popups.FillProps;

/// <summary>
/// One rule of a fill set in the fill popup: where it goes, how thick, and which props it picks from.
/// </summary>
public partial class FillRuleModel : PropertyChangedUpdater
{
    #region Constructor

    public FillRuleModel(FillRule rule, IEnumerable<PropChoiceModel> props, string[] areaNames, Action<FillRuleModel> remove)
    {
        Rule = rule;
        AreaNames = areaNames;
        Pool = [.. props.Select(x => new PoolItemModel(x, rule))];
        RemoveCommand = new RelayCommand(() => remove(this));
    }

    #endregion

    #region Properties

    public FillRule Rule { get; }

    public PoolItemModel[] Pool { get; }

    // the named areas on this map, for InsideArea
    public string[] AreaNames { get; }

    public FillPlace[] Places { get; } = Enum.GetValues<FillPlace>();

    public FillPlace Place
    {
        get => Rule.Place;
        set
        {
            if (Rule.Place == value) return;
            Rule.Place = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsInsideArea));
        }
    }

    public bool IsInsideArea => Rule.Place == FillPlace.InsideArea;

    public string GroundColor
    {
        get => Rule.GroundColor;
        set
        {
            if (Rule.GroundColor == value) return;
            Rule.GroundColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Swatch));
        }
    }

    // the ground colour drawn next to its text box, see-through when the rule takes any ground
    public Brush Swatch
    {
        get
        {
            var hex = Rule.GroundColor.Trim().TrimStart('#');
            if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var value)) return new SolidColorBrush(Colors.Transparent);
            return new SolidColorBrush(ColorHelper.FromArgb(255, (byte) (value >> 16), (byte) (value >> 8), (byte) value));
        }
    }

    // the sliders and number boxes work in doubles
    public double Density
    {
        get => Rule.Density;
        set => Rule.Density = (int) Math.Clamp(value, 0, 100);
    }

    public double Spacing
    {
        get => Rule.Spacing;
        set => Rule.Spacing = double.IsNaN(value) ? 48 : (int) Math.Clamp(value, 4, 1000);
    }

    public double ClumpSize
    {
        get => Rule.ClumpSize;
        set => Rule.ClumpSize = double.IsNaN(value) ? 1 : (int) Math.Clamp(value, 1, 20);
    }

    public double ColorTolerance
    {
        get => Rule.ColorTolerance;
        set => Rule.ColorTolerance = (int) Math.Clamp(value, 0, 255);
    }

    public RelayCommand RemoveCommand { get; }

    #endregion
}

/// <summary>
/// A prop that a rule can pick from, ticked when it is in the rule's pool.
/// </summary>
public partial class PoolItemModel(PropChoiceModel choice, FillRule rule) : PropertyChangedUpdater
{
    #region Properties

    public PropChoiceModel Choice => choice;

    public bool IsInPool
    {
        get => rule.Props.Contains(choice.Prop.Guid);
        set
        {
            if (value == IsInPool) return;
            if (value) rule.Props.Add(choice.Prop.Guid);
            else rule.Props.Remove(choice.Prop.Guid);
            OnPropertyChanged();
        }
    }

    #endregion
}
