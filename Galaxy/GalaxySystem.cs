namespace GameProject.World;

public sealed class GalaxySystem
{
    private readonly List<string> _connections = new();

    public GalaxySystem(
        string id,
        string name,
        float mapX,
        float mapY,
        int seed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name;
        MapX = mapX;
        MapY = mapY;
        Seed = seed;
    }

    public string Id { get; }
    public string Name { get; }

    public float MapX { get; }
    public float MapY { get; }

    public int Seed { get; }

    public IReadOnlyList<string> Connections => _connections;

    internal void AddConnection(string systemId)
    {
        if (!_connections.Contains(systemId))
            _connections.Add(systemId);
    }
}