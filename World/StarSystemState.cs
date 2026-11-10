namespace GameProject.World;

public sealed class StarSystemState
{
    internal StarSystemState(GalaxySystem definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Definition = definition;
    }

    public GalaxySystem Definition { get; }

    public string SystemId => Definition.Id;

    public long LastSimulatedDay { get; private set; }

    public TimeSpan LastSimulatedAt { get; private set; }

    internal void CompleteDay(
        long dayNumber,
        TimeSpan elapsedWorldTime)
    {
        if (dayNumber != LastSimulatedDay + 1)
        {
            throw new InvalidOperationException(
                $"Unexpected simulation day for system '{SystemId}'.");
        }

        if (elapsedWorldTime <= LastSimulatedAt)
        {
            throw new InvalidOperationException(
                $"Simulation time did not advance for system '{SystemId}'.");
        }

        LastSimulatedDay = dayNumber;
        LastSimulatedAt = elapsedWorldTime;
    }
}