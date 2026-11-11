using GameProject.Ships;
using GameProject.World;

namespace GameProject.NPC;

public static class NpcPopulationGenerator
{
    public static IReadOnlyList<NpcState> Generate(
        int worldSeed,
        GalaxyMap galaxy,
        IReadOnlyCollection<NpcArchetypeDefinition> archetypes,
        IReadOnlyDictionary<string, ShipDefinition> shipDefinitions,
        NpcPopulationSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(archetypes);
        ArgumentNullException.ThrowIfNull(shipDefinitions);

        settings ??= new NpcPopulationSettings();
        settings.Validate();

        var systems = galaxy.Systems
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();

        if (systems.Length == 0)
        {
            throw new InvalidOperationException(
                "Cannot generate NPC population in an empty galaxy.");
        }

        var orderedArchetypes = archetypes
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .ToArray();

        ValidateArchetypes(
            orderedArchetypes,
            shipDefinitions);

        var result = new List<NpcState>();

        foreach (var system in systems)
        {
            var countRandom = CreateRandom(
                worldSeed,
                system.Id,
                0,
                "population-count");

            var npcCount = NextInt(
                settings.MinimumPerSystem,
                settings.MaximumPerSystem,
                countRandom);

            var rotationRandom = CreateRandom(
                worldSeed,
                system.Id,
                0,
                "spawn-rotation");

            var rotation =
                rotationRandom.NextDouble() * Math.Tau;

            for (var i = 0; i < npcCount; i++)
            {
                var archetypeRandom = CreateRandom(
                    worldSeed,
                    system.Id,
                    i,
                    "archetype");

                var archetype = SelectArchetype(
                    orderedArchetypes,
                    archetypeRandom);

                var identity = NpcNameGenerator.Generate(
                    worldSeed,
                    system.Id,
                    i);

                var locationRandom = CreateRandom(
                    worldSeed,
                    system.Id,
                    i,
                    "location");

                var location = GenerateLocation(
                    system.Id,
                    i,
                    npcCount,
                    rotation,
                    settings,
                    locationRandom);

                var stateRandom = CreateRandom(
                    worldSeed,
                    system.Id,
                    i,
                    "state");

                var npc = NpcFactory.Create(
                    archetype,
                    shipDefinitions,
                    identity,
                    location,
                    stateRandom);

                result.Add(npc);
            }
        }

        return result.AsReadOnly();
    }

    private static void ValidateArchetypes(
        NpcArchetypeDefinition[] archetypes,
        IReadOnlyDictionary<string, ShipDefinition> shipDefinitions)
    {
        if (archetypes.Length == 0)
        {
            throw new InvalidDataException(
                "No NPC archetypes are available.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var archetype in archetypes)
        {
            NpcArchetypeDefinitionValidator.Validate(archetype);

            if (!ids.Add(archetype.Id))
            {
                throw new InvalidDataException(
                    $"Duplicate NPC archetype '{archetype.Id}'.");
            }

            foreach (var option in archetype.ShipOptions)
            {
                if (!shipDefinitions.TryGetValue(
                        option.ShipDefinitionId,
                        out var ship))
                {
                    throw new InvalidDataException(
                        $"NPC archetype '{archetype.Id}' references " +
                        $"unknown ship '{option.ShipDefinitionId}'.");
                }

                ShipDefinitionValidator.Validate(ship);
            }
        }
    }

    private static NpcArchetypeDefinition SelectArchetype(
        IReadOnlyList<NpcArchetypeDefinition> archetypes,
        NpcRandom random)
    {
        double totalWeight = 0.0;

        foreach (var archetype in archetypes)
            totalWeight += archetype.SpawnWeight;

        if (!double.IsFinite(totalWeight) || totalWeight <= 0.0)
        {
            throw new InvalidDataException(
                "NPC archetypes have invalid total spawn weight.");
        }

        var roll = random.NextDouble() * totalWeight;

        foreach (var archetype in archetypes)
        {
            roll -= archetype.SpawnWeight;

            if (roll < 0.0)
                return archetype;
        }

        return archetypes[^1];
    }

    private static NpcLocation GenerateLocation(
        string systemId,
        int npcIndex,
        int npcCount,
        double rotation,
        NpcPopulationSettings settings,
        NpcRandom random)
    {
        var angle = rotation +
                    Math.Tau * npcIndex / npcCount +
                    (random.NextDouble() - 0.5) * 0.15;

        // Равномерное распределение точек по площади кольца.
        var minSquared =
            settings.MinimumSpawnDistance *
            settings.MinimumSpawnDistance;

        var maxSquared =
            settings.MaximumSpawnDistance *
            settings.MaximumSpawnDistance;

        var radius = Math.Sqrt(
            minSquared +
            random.NextDouble() * (maxSquared - minSquared));

        return new NpcLocation(
            systemId,
            Math.Cos(angle) * radius,
            Math.Sin(angle) * radius);
    }

    private static NpcRandom CreateRandom(
        int worldSeed,
        string systemId,
        int npcIndex,
        string purpose)
    {
        return new NpcRandom(
            NpcDeterminism.CreateSeed(
                worldSeed,
                systemId,
                npcIndex,
                purpose));
    }

    private static int NextInt(
        int minimum,
        int maximum,
        NpcRandom random)
    {
        var range = (long)maximum - minimum + 1L;

        var offset = (long)Math.Floor(
            random.NextDouble() * range);

        return checked(minimum + (int)offset);
    }
}