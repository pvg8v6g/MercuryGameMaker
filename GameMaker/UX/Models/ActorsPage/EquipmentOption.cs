using GameLibrary.Models.Items;

namespace GameMaker.UX.Models.ActorsPage;

public class EquipmentOption(Equipment? equipment)
{
    public string Name { get; } = equipment?.Name ?? "- None -";

    public int Icon { get; } = equipment?.Icon ?? 0;

    public Guid? Guid { get; } = equipment?.Guid ?? System.Guid.Empty;
}
