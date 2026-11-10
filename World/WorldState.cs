
using System;
using System.Collections.Generic;

namespace GameProject.World;

public sealed class WorldState
{
    private readonly Dictionary<string, StarSystem> _starSystems =
        new(StringComparer.OrdinalIgnoreCase);

    private WorldState(Guid id, int seed)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "World ID cannot be empty.",
                nameof(id));

        Id = id;
        Seed = seed;

        Galaxy = GalaxyGenerator.Generate(seed);

        var startingSystem = Galaxy.GetSystem("sol")
            ?? throw new InvalidOperationException(
                "Starting galaxy system was not generated.");

        // Начальная система создаётся сразу.
        var startingStarSystem = StarSystemGenerator.Generate(
            startingSystem.Seed);

        _starSystems.Add(
            startingSystem.Id,
            startingStarSystem);

        CurrentSystemId = startingSystem.Id;
    }

    public Guid Id { get; }

    public int Seed { get; }

    public GalaxyMap Galaxy { get; }

    // ID текущей системы.
    public string CurrentSystemId { get; private set; }

    // Состояние текущей звёздной системы.
    public StarSystem StarSystem =>
        _starSystems[CurrentSystemId];

    // Количество систем, состояние которых уже создано.
    public int VisitedSystemCount => _starSystems.Count;

    public void EnterSystem(string systemId)
    {
        var system = Galaxy.GetSystem(systemId);

        if (system is null)
        {
            throw new ArgumentException(
                $"Unknown galaxy system: {systemId}",
                nameof(systemId));
        }

        // Генерируем систему только при первом посещении.
        if (!_starSystems.TryGetValue(
                system.Id,
                out _))
        {
            var starSystem = StarSystemGenerator.Generate(
                system.Seed);

            _starSystems.Add(
                system.Id,
                starSystem);
        }

        // При повторном посещении используем сохранённый экземпляр.
        CurrentSystemId = system.Id;
    }

    public static WorldState Create(
        WorldStartData startData)
    {
        return new WorldState(
            Guid.NewGuid(),
            startData.Seed);
    }

    public static WorldState CreateNew()
    {
        return Create(WorldStartData.CreateNew());
    }
}
