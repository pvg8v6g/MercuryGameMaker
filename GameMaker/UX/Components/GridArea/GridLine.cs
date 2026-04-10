namespace GameMaker.UX.Components.GridArea;

public sealed class GridLine(double x1, double y1, double x2, double y2)
{
    public double X1 { get; set; } = x1;
    public double Y1 { get; set; } = y1;
    public double X2 { get; set; } = x2;
    public double Y2 { get; set; } = y2;
}
