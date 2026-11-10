namespace GameProject.Ships;

public sealed class EquipmentLevelDefinition
{
    public int Level { get; init; }

    public EquipmentStatRanges Stats { get; init; } = new();
}

public sealed class EquipmentStatRanges
{
    public EquipmentFloatRange MaxSpeedBonus { get; init; } = new();

    public EquipmentIntRange FuelCapacityBonus { get; init; } = new();
}

public sealed class EquipmentFloatRange
{
    public float Min { get; init; }

    public float Max { get; init; }
}

public sealed class EquipmentIntRange
{
    public int Min { get; init; }

    public int Max { get; init; }
}