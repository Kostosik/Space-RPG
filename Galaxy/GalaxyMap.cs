namespace GameProject.World;

public sealed class GalaxyMap
{
    private readonly Dictionary<string, GalaxySystem> _systems =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<GalaxySystem> Systems =>
        _systems.Values;

    public void AddSystem(GalaxySystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        if (!_systems.TryAdd(system.Id, system))
        {
            throw new InvalidOperationException(
                $"Galaxy system '{system.Id}' already exists.");
        }
    }

    public GalaxySystem? GetSystem(string id)
    {
        return _systems.TryGetValue(id, out var system)
            ? system
            : null;
    }

    public bool Connect(string firstId, string secondId)
    {
        var first = GetSystem(firstId);
        var second = GetSystem(secondId);

        if (first is null ||
            second is null ||
            firstId.Equals(
                secondId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        first.AddConnection(secondId);
        second.AddConnection(firstId);

        return true;
    }

    public bool CanTravel(string fromId, string toId)
    {
        var system = GetSystem(fromId);

        return system is not null &&
               system.Connections.Contains(toId);
    }
}