using GameLibrary.Enumerations;

namespace GameLibrary.Utilities.Calculations;

public static class Calculations
{
    #region Fields

    private static readonly Random Random = new();

    #endregion

    #region Numbers

    public static int GetNextId(int[] ids, int minimum = 1)
    {
        var min = minimum;
        var max = ids.Length > 0 ? ids.Max(x => x) : min;
        for (var i = min; i <= max + 1; i++)
        {
            if (ids.Contains(i)) continue;
            min = i;
            break;
        }

        return min;
    }

    public static double RandomBetween(double lower, double upper)
    {
        return lower + (Random.NextDouble() * (upper - lower));
    }

    public static int RoundToMultiple(double value, int multiple, Rounding rounding)
    {
        return rounding switch
        {
            Rounding.Down => (int) (Math.Floor(value / multiple) * multiple),
            Rounding.Up => (int) (Math.Ceiling(value / multiple) * multiple),
            Rounding.Default => (int) (Math.Round(value / multiple) * multiple),
            _ => throw new ArgumentOutOfRangeException(nameof(rounding), rounding, null)
        };
    }

    #endregion

    #region Growth

    // a growth's value at a level: min at level 1, max at the level cap, rate bends the line into a curve
    public static double GetLevelValue(double level, double rate, double min, double max, int levelCap)
    {
        var span = Math.Max(1.0d, levelCap - 1.0d);
        var ceil1 = Math.Ceiling((max - min) * (level - 1.0d) / span);
        var ceil2 = Math.Ceiling((max - min) * Math.Pow((level - 1.0d) / span, 2.0d));
        return min + Math.Floor((1.0d - rate) * ceil1 + rate * ceil2);
    }

    #endregion
}
