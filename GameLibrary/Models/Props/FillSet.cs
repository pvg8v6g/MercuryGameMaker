using System.Collections.ObjectModel;

namespace GameLibrary.Models.Props;

/// <summary>
/// A reusable way of filling a map with props, one per kind of place (Forest, Desert, Snow, Farm).
/// Its rules run in order, each one keeps clear of the props already placed.
/// </summary>
public partial class FillSet : BaseModel
{
    #region Properties

    public ObservableCollection<FillRule> Rules { get; init; } = [];

    #endregion
}
