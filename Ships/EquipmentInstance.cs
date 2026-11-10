namespace GameProject.Ships;

/// <summary>
/// A unique instance of an equipment item.
/// Its generated statistics do not change when it is moved
/// between a shop, inventory or a ship slot.
/// </summary>
public sealed class EquipmentInstance
{
    internal EquipmentInstance(
        Guid instanceId,
        string definitionId,
        int level,
        EquipmentStats stats)
    {
        if (instanceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Equipment instance ID cannot be empty.",
                nameof(instanceId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(definitionId);

        if (level <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(level));
        }

        InstanceId = instanceId;
        DefinitionId = definitionId;
        Level = level;
        Stats = stats;
    }

    public Guid InstanceId { get; }

    public string DefinitionId { get; }

    public int Level { get; }

    public EquipmentStats Stats { get; }
}