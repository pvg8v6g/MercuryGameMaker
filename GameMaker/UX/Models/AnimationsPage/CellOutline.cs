namespace GameMaker.UX.Models.AnimationsPage;

// Label is the cell's id, shown in the outline's top right corner. LabelX moves it left when another cell's is there
public record CellOutline(double X, double Y, double Width, double Height, string Label, double LabelX);
