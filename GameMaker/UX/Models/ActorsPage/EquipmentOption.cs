using GameLibrary.Models;
using GameLibrary.Models.Items;

namespace GameMaker.UX.Models.ActorsPage;

public class EquipmentOption(Equipment? equipment)
{
    public string Name { get; } = equipment?.Name ?? "- None -";

    public GameIcon Icon { get; } = equipment?.Icon ?? GameIcon.None;

    public Guid? Guid { get; } = equipment?.Guid ?? System.Guid.Empty;
}
