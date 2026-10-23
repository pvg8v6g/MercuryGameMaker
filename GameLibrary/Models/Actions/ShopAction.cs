using System.Collections.ObjectModel;

namespace GameLibrary.Models.Actions;

public partial class ShopAction : PageAction
{
    public bool CanExit
    {
        get;
        set => SetField(ref field, value);
    }

    public bool CanSell
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public ObservableCollection<Guid> ShopItems { get; init; } = [];

    public override string ToString() => "Open Shop";
}
