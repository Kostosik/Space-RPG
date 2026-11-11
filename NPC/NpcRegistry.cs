namespace GameProject.NPC;

public sealed class NpcRegistry
{
    private readonly Dictionary<Guid, NpcState> _npcs = new();

    public int Count => _npcs.Count;

    public IReadOnlyCollection<NpcState> All => _npcs.Values;

    public bool TryAdd(NpcState npc)
    {
        ArgumentNullException.ThrowIfNull(npc);

        return _npcs.TryAdd(npc.Id, npc);
    }

    public void AddRange(IEnumerable<NpcState> npcs)
    {
        ArgumentNullException.ThrowIfNull(npcs);

        var batch = npcs.ToArray();
        var ids = new HashSet<Guid>();

        foreach (var npc in batch)
        {
            ArgumentNullException.ThrowIfNull(npc);

            if (_npcs.ContainsKey(npc.Id) ||
                !ids.Add(npc.Id))
            {
                throw new InvalidOperationException(
                    $"Duplicate NPC ID '{npc.Id}'.");
            }
        }

        foreach (var npc in batch)
            _npcs.Add(npc.Id, npc);
    }

    public NpcState? Get(Guid id)
    {
        return _npcs.TryGetValue(id, out var npc)
            ? npc
            : null;
    }

    public bool TryGet(
        Guid id,
        out NpcState? npc)
    {
        return _npcs.TryGetValue(id, out npc);
    }

    public bool Remove(Guid id)
    {
        return _npcs.Remove(id);
    }
}