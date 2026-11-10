namespace GameProject.World;

public sealed class GalaxySystemState
{
    internal GalaxySystemState(GalaxySystem definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
    }

    // Неизменяемое описание системы из GalaxyMap.
    public GalaxySystem Definition { get; }

    public string SystemId => Definition.Id;

    // Последний игровой день, обработанный глобальной симуляцией.
    public long LastGlobalSimulationDay { get; private set; }

    internal void AdvanceGlobalDay(long dayNumber)
    {
        if (dayNumber != LastGlobalSimulationDay + 1)
        {
            throw new InvalidOperationException(
                $"Unexpected simulation day for system '{SystemId}'.");
        }

        LastGlobalSimulationDay = dayNumber;
    }
}