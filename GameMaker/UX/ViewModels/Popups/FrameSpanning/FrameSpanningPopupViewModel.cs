using System.Collections.ObjectModel;
using GameLibrary.Models.Animations;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.ViewModels.Popups.FrameSpanning;

public class FrameSpanningPopupViewModel(int startFrame) : PropertyChangedUpdater
{
    #region Properties

    public double StartFrame
    {
        get;
        set => SetField(ref field, value);
    } = startFrame;

    public double EndFrame
    {
        get;
        set => SetField(ref field, value);
    } = startFrame;

    public double StartCell
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public double EndCell
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public bool Tween
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public bool All
    {
        get;
        set
        {
            if (!SetField(ref field, value)) return;
            X = value;
            Y = value;
            ScaleX = value;
            ScaleY = value;
            Hue = value;
            Contrast = value;
            Brightness = value;
            Saturation = value;
            Opacity = value;
            Rotate = value;
            BlendMode = value;
        }
    }

    public bool X
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Y
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ScaleX
    {
        get;
        set => SetField(ref field, value);
    }

    public bool ScaleY
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Hue
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Contrast
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Brightness
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Saturation
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Opacity
    {
        get;
        set => SetField(ref field, value);
    }

    public bool Rotate
    {
        get;
        set => SetField(ref field, value);
    }

    public bool BlendMode
    {
        get;
        set => SetField(ref field, value);
    }

    #endregion

    #region Public methods

    public void DeleteCells(ObservableCollection<AnimationEffect> effects)
    {
        var toRemove = effects
            .Where(x => x.Frame >= StartFrame && x.Frame <= EndFrame)
            .Where(x => x.Cell >= StartCell && x.Cell <= EndCell)
            .ToList();

        foreach (var effect in toRemove) effects.Remove(effect);
    }

    public void ApplySpanning(ObservableCollection<AnimationEffect> effects)
    {
        var cellIds = effects
            .Where(x => x.Frame >= StartFrame && x.Frame <= EndFrame)
            .Where(x => x.Cell >= StartCell && x.Cell <= EndCell)
            .Select(x => x.Cell)
            .Distinct()
            .ToList();

        var startCells = GetStartCells(effects);
        var endCells = GetEndCells(effects);

        foreach (var cellId in cellIds)
        {
            var start = startCells.FirstOrDefault(x => x.Cell == cellId);
            var end = endCells.FirstOrDefault(x => x.Cell == cellId);
            if (start is null || end is null) continue;

            var frameCount = end.Frame - start.Frame;
            if (frameCount == 0) continue;

            var index = 0;
            for (var f = start.Frame; f <= end.Frame; f++)
            {
                var cell = effects.FirstOrDefault(x => x.Frame == f && x.Cell == cellId);
                if (cell is not null) ApplyTransition(cell, start, end, frameCount, index);
                index++;
            }
        }
    }

    #endregion

    #region Private methods

    private List<AnimationEffect> GetStartCells(ObservableCollection<AnimationEffect> effects)
    {
        var result = new List<AnimationEffect>();
        for (var f = (int) StartFrame; f <= (int) EndFrame; f++)
        {
            var cells = effects.Where(e => e.Frame == f);
            result.AddRange(cells.Where(c => result.All(r => r.Cell != c.Cell)));
        }

        return result;
    }

    private List<AnimationEffect> GetEndCells(ObservableCollection<AnimationEffect> effects)
    {
        var result = new List<AnimationEffect>();
        for (var f = (int) EndFrame; f >= (int) StartFrame; f--)
        {
            var cells = effects.Where(e => e.Frame == f);
            result.AddRange(cells.Where(c => result.All(r => r.Cell != c.Cell)));
        }

        return result;
    }

    private void ApplyTransition(AnimationEffect cell, AnimationEffect start, AnimationEffect end, int frameCount,
        int index)
    {
        if (!Tween)
        {
            if (X) cell.X = start.X;
            if (Y) cell.Y = start.Y;
            if (ScaleX) cell.ScaleX = start.ScaleX;
            if (ScaleY) cell.ScaleY = start.ScaleY;
            if (Hue) cell.Hue = start.Hue;
            if (Contrast) cell.Contrast = start.Contrast;
            if (Brightness) cell.Brightness = start.Brightness;
            if (Saturation) cell.Saturation = start.Saturation;
            if (Opacity) cell.Opacity = start.Opacity;
            if (Rotate) cell.Rotate = start.Rotate;
            if (BlendMode) cell.BlendMode = start.BlendMode;
            return;
        }

        if (X) cell.X = start.X + (end.X - start.X) / frameCount * index;
        if (Y) cell.Y = start.Y + (end.Y - start.Y) / frameCount * index;
        if (ScaleX) cell.ScaleX = start.ScaleX + (end.ScaleX - start.ScaleX) / frameCount * index;
        if (ScaleY) cell.ScaleY = start.ScaleY + (end.ScaleY - start.ScaleY) / frameCount * index;
        if (Hue) cell.Hue = start.Hue + (end.Hue - start.Hue) / frameCount * index;
        if (Contrast) cell.Contrast = start.Contrast + (end.Contrast - start.Contrast) / frameCount * index;
        if (Brightness) cell.Brightness = start.Brightness + (end.Brightness - start.Brightness) / frameCount * index;
        if (Saturation) cell.Saturation = start.Saturation + (end.Saturation - start.Saturation) / frameCount * index;
        if (Opacity) cell.Opacity = start.Opacity + (end.Opacity - start.Opacity) / frameCount * index;
        if (Rotate) cell.Rotate = start.Rotate + (end.Rotate - start.Rotate) / frameCount * index;
        if (BlendMode) cell.BlendMode = start.BlendMode;
    }

    #endregion
}
