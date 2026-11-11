namespace GameProject.NPC;

public sealed class NpcState
{
    public NpcMovementState Movement { get; } = new();
    public NpcState(
        NpcIdentity identity,
        string archetypeId,
        NpcPersonality personality,
        NpcShipState ship,
        NpcLocation location,
        NpcEconomyState economy)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(personality);
        ArgumentNullException.ThrowIfNull(ship);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(economy);

        ArgumentException.ThrowIfNullOrWhiteSpace(archetypeId);

        if (!string.Equals(
                archetypeId,
                archetypeId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Archetype ID cannot contain surrounding whitespace.",
                nameof(archetypeId));
        }

        Identity = identity;
        ArchetypeId = archetypeId;
        Personality = personality;
        Ship = ship;
        Location = location;
        Economy = economy;
    }

    public Guid Id => Identity.Id;

    public NpcIdentity Identity { get; }

    public string ArchetypeId { get; }

    public NpcPersonality Personality { get; }

    public NpcShipState Ship { get; private set; }

    public NpcLocation Location { get; private set; }

    public NpcEconomyState Economy { get; }

    public void SetLocation(NpcLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        Location = location;
    }

    public void ReplaceShip(NpcShipState ship)
    {
        ArgumentNullException.ThrowIfNull(ship);
        Ship = ship;
    }
}