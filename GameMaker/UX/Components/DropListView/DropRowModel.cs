using GameLibrary.Models.Fighter;
using GameLibrary.Models.Items;
using MercuryLibrary.WinUI3Components;

namespace GameMaker.UX.Components.DropListView;

public class DropRowModel(DropModel drop, Item? item) : PropertyChangedUpdater
{
    public DropModel Drop => drop;

    public int Icon => item?.Icon ?? 0;

    public string Name => item?.Name ?? "<Unknown>";

    public string PercentDisplay => $"{drop.DropPercent}%";
}
