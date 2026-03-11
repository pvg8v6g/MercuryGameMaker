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

    #endregion

    #region Growth

    public static double GetLevelValue(double level, double rate, double min, double max)
    {
        var ceil1 = Math.Ceiling((max - min) * (level - 1.0d) / 98.0d);
        var ceil2 = Math.Ceiling((max - min) * Math.Pow((level - 1.0d) / 98.0d, 2.0d));
        return min + Math.Floor((1.0d - rate) * ceil1 + rate * ceil2);
    }

    #endregion
}
