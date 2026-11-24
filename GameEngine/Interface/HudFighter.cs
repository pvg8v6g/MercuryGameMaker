using System.Collections.ObjectModel;
using Windows.Foundation;
using MercuryLibrary.WinUI3Components;
using Microsoft.UI.Xaml.Media;

namespace GameEngine.Interface;

/// <summary>
/// One fighter on the battle HUD: a party member's panel, an enemy's Life bar over its head, or the boss bar. Made and
/// changed on the ui thread only. The bars fill by a fraction (0 to 1), so they work at any width.
/// </summary>
public partial class HudFighter(string name, bool isLeader) : PropertyChangedUpdater
{
    #region Properties

    public string Name => name;

    public bool IsLeader => isLeader;

    // the face picture, cut out of its sheet: Face is the whole sheet, FaceRect the cell to show
    public ImageSource? Face
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(HasFace));
        }
    }

    public Rect FaceRect
    {
        get;
        set
        {
            SetField(ref field, value);
            OnPropertyChanged(nameof(FaceClip));
            OnPropertyChanged(nameof(FaceLeft));
            OnPropertyChanged(nameof(FaceTop));
            OnPropertyChanged(nameof(FaceWidth));
            OnPropertyChanged(nameof(FaceHeight));
        }
    }

    public bool HasFace => Face is not null;

    public Rect FaceClip => new(0, 0, FaceRect.Width, FaceRect.Height);

    public double FaceLeft => -FaceRect.X;

    public double FaceTop => -FaceRect.Y;

    public double FaceWidth => Math.Max(1, FaceRect.Width);

    public double FaceHeight => Math.Max(1, FaceRect.Height);

    public int Life { get; private set; }

    public int MaxLife { get; private set; }

    public int Mana { get; private set; }

    public int MaxMana { get; private set; }

    public double LifeFraction => MaxLife > 0 ? Math.Clamp(Life / (double) MaxLife, 0, 1) : 0;

    public double ManaFraction => MaxMana > 0 ? Math.Clamp(Mana / (double) MaxMana, 0, 1) : 0;

    public string LifeText => $"{Life} / {MaxLife}";

    public string ManaText => $"{Mana} / {MaxMana}";

    // the states on it, in the order they went on
    public ObservableCollection<HudState> States { get; } = [];

    // an enemy's bar: where it floats over the head, in scaled map pixels on the layer the camera moves
    public double BarX { get; private set; }

    public double BarY { get; private set; }

    // an enemy's bar only shows once the player has inspected that enemy (or while testing)
    public bool ShowsBar
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Methods

    // only what changed is announced, the HUD updates every frame
    public void Set(int life, int maxLife, int mana, int maxMana)
    {
        if (life != Life || maxLife != MaxLife)
        {
            Life = life;
            MaxLife = maxLife;
            OnPropertyChanged(nameof(Life));
            OnPropertyChanged(nameof(MaxLife));
            OnPropertyChanged(nameof(LifeFraction));
            OnPropertyChanged(nameof(LifeText));
        }

        if (mana != Mana || maxMana != MaxMana)
        {
            Mana = mana;
            MaxMana = maxMana;
            OnPropertyChanged(nameof(Mana));
            OnPropertyChanged(nameof(MaxMana));
            OnPropertyChanged(nameof(ManaFraction));
            OnPropertyChanged(nameof(ManaText));
        }
    }

    // the rows are only touched when a state goes on or off, the wipes move every frame. create makes a state's icon
    public void SetStates(HudStateValue[] values, Func<Guid, HudState> create)
    {
        for (var i = States.Count - 1; i >= 0; i--)
        {
            if (values.All(x => x.Id != States[i].Id)) States.RemoveAt(i);
        }

        foreach (var value in values)
        {
            var row = States.FirstOrDefault(x => x.Id == value.Id);
            if (row is null)
            {
                row = create(value.Id);
                States.Add(row);
            }

            row.Set(value.Fraction, value.Stacks);
        }
    }

    public void MoveBar(double x, double y)
    {
        if (Math.Abs(x - BarX) < 0.01 && Math.Abs(y - BarY) < 0.01) return;

        BarX = x;
        BarY = y;
        OnPropertyChanged(nameof(BarX));
        OnPropertyChanged(nameof(BarY));
    }

    #endregion
}
