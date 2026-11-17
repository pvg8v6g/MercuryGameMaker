using GameLibrary.Models.Props;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GameMaker.UX.Models.MainPage;

/// <summary>
/// One prop in the editor's props palette: the catalogue prop and its picture.
/// </summary>
public class PropChoiceModel(Prop prop, string imagePath, BitmapImage? image, double width, double height)
{
    #region Properties

    public Prop Prop => prop;

    public string Name => string.IsNullOrEmpty(prop.Name) ? prop.ImageName ?? "Prop" : prop.Name;

    public string ImagePath => imagePath;

    public BitmapImage? Image => image;

    public double Width => width;

    public double Height => height;

    #endregion
}
