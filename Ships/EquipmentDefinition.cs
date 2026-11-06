
using GameProject.Content;

namespace GameProject.Ships;

public sealed class EquipmentDefinition : IGameDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public float MaxSpeedBonus { get; init; }

    public int FuelCapacityBonus { get; init; }
}
