using GameLibrary.Utilities.ComponentModels;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Models.DisciplinesPage;

public class AttributeGrowthSetting : PropertyChangedUpdater
{
    public CroppedImage IconImage { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public Guid AttributeGuid { get; set; }

    public Guid GrowthGuid { get; set; }
}
