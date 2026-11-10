
using System;
using System.Collections.Generic;

namespace GameProject.World;

public sealed class WorldState
{
    private sealed class StarSystemState
    {
        public StarSystemState(StarSystem starSystem)
        {
            StarSystem = starSystem;
        }

        public StarSystem StarSystem { get; }

        public TimeSpan LastSimulatedAt { get; set; }
    }

    private readonly Dictionary<string, StarSystemState> _starSystems =
        new(StringComparer.OrdinalIgnoreCase);

    private TimeSpan _worldElapsedTime;

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

        _starSystems.Add(
            startingSystem.Id,
            new StarSystemState(
                StarSystemGenerator.Generate(startingSystem.Seed)));

        CurrentSystemId = startingSystem.Id;
    }

    public Guid Id { get; }

    public int Seed { get; }

    public GalaxyMap Galaxy { get; }

    public string CurrentSystemId { get; private set; }

    public StarSystem StarSystem =>
        _starSystems[CurrentSystemId].StarSystem;

    public int VisitedSystemCount => _starSystems.Count;

    /// <summary>
    /// Advances the active system to the specified global game time.
    /// Inactive systems are updated lazily when entered.
    /// </summary>
    public void AdvanceSimulationTo(TimeSpan elapsedWorldTime)
    {
        if (elapsedWorldTime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(elapsedWorldTime));

        if (elapsedWorldTime < _worldElapsedTime)
            throw new ArgumentOutOfRangeException(
                nameof(elapsedWorldTime),
                "World time cannot move backwards.");

        if (elapsedWorldTime == _worldElapsedTime)
            return;

        var currentState = _starSystems[CurrentSystemId];

        AdvanceSystemTo(
            currentState,
            elapsedWorldTime);

        _worldElapsedTime = elapsedWorldTime;
    }

    public void EnterSystem(string systemId)
    {
        var system = Galaxy.GetSystem(systemId);

        if (system is null)
        {
            throw new ArgumentException(
                $"Unknown galaxy system: {systemId}",
                nameof(systemId));
        }

        if (!_starSystems.TryGetValue(
                system.Id,
                out var state))
        {
            state = new StarSystemState(
                StarSystemGenerator.Generate(system.Seed));

            _starSystems.Add(system.Id, state);
        }

        // Восстанавливаем систему относительно глобального времени.
        AdvanceSystemTo(
            state,
            _worldElapsedTime);

        CurrentSystemId = system.Id;
    }

    private static void AdvanceSystemTo(
        StarSystemState state,
        TimeSpan targetTime)
    {
        var elapsed = targetTime - state.LastSimulatedAt;

        if (elapsed < TimeSpan.Zero)
            throw new InvalidOperationException(
                "System simulation time cannot move backwards.");

        if (elapsed > TimeSpan.Zero)
        {
            state.StarSystem.AdvanceOrbit(
                elapsed.TotalDays);
        }

        state.LastSimulatedAt = targetTime;
    }

    public static WorldState Create(WorldStartData startData)
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
