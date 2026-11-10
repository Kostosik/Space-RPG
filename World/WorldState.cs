using GameProject.NPC;
using System;
using System.Collections.Generic;

namespace GameProject.World;

public sealed class WorldState
{
    public NpcRegistry Npcs { get; } = new();
    private sealed class LocalStarSystemState
    {
        public LocalStarSystemState(StarSystem starSystem)
        {
            ArgumentNullException.ThrowIfNull(starSystem);

            StarSystem = starSystem;
        }

        public StarSystem StarSystem { get; }

        public TimeSpan LastSimulatedAt { get; private set; }

        public void AdvanceTo(TimeSpan targetTime)
        {
            if (targetTime < LastSimulatedAt)
            {
                throw new InvalidOperationException(
                    "Local system time cannot move backwards.");
            }

            var elapsed = targetTime - LastSimulatedAt;

            if (elapsed <= TimeSpan.Zero)
                return;

            StarSystem.AdvanceOrbit(elapsed.TotalDays);
            LastSimulatedAt = targetTime;
        }
    }

    private readonly Dictionary<string, GalaxySystemState> _systemStates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, LocalStarSystemState> _localSystemStates =
        new(StringComparer.OrdinalIgnoreCase);

    private string _currentSystemId = string.Empty;

    private TimeSpan _elapsedWorldTime;

    private long _completedDayNumber;

    private WorldState(Guid id, WorldStartData startData)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "World ID cannot be empty.",
                nameof(id));
        }

        Id = id;
        Seed = startData.Seed;

        Galaxy = GalaxyGenerator.Generate(
            startData.Seed,
            startData.GalaxySystemCount);

        // Глобальное состояние создаётся для каждой системы.
        foreach (var system in Galaxy.Systems)
        {
            _systemStates.Add(
                system.Id,
                new GalaxySystemState(system));
        }

        var startingSystem = Galaxy.GetSystem("sol")
            ?? throw new InvalidOperationException(
                "Starting galaxy system was not generated.");

        // Локальное представление создаётся только для стартовой системы.
        _currentSystemId = startingSystem.Id;

        _localSystemStates.Add(
            startingSystem.Id,
            new LocalStarSystemState(
                StarSystemGenerator.Generate(
                    startingSystem.Id,
                    startingSystem.Seed)));
    }

    public Guid Id { get; }

    public int Seed { get; }

    public GalaxyMap Galaxy { get; }

    public string CurrentSystemId => _currentSystemId;

    // Подробное представление активной системы.
    public StarSystem StarSystem =>
        _localSystemStates[_currentSystemId].StarSystem;

    // Глобальное состояние всех систем галактики.
    public IReadOnlyDictionary<string, GalaxySystemState> SystemStates =>
        _systemStates;

    public int SystemCount => _systemStates.Count;

    // Число систем, для которых создано локальное представление.
    public int VisitedSystemCount => _localSystemStates.Count;

    public long CompletedDayNumber => _completedDayNumber;

    public TimeSpan ElapsedWorldTime => _elapsedWorldTime;

    public GalaxySystemState? GetSystemState(string systemId)
    {
        return _systemStates.TryGetValue(
            systemId,
            out var state)
                ? state
                : null;
    }

    internal void AdvanceLocalSimulation(
        TimeSpan previousElapsed,
        TimeSpan currentElapsed)
    {
        if (previousElapsed != _elapsedWorldTime)
        {
            throw new InvalidOperationException(
                "Game time is out of sync with world time.");
        }

        if (currentElapsed < previousElapsed)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentElapsed));
        }

        // Продвигается только активная локальная система.
        var currentState = _localSystemStates[_currentSystemId];

        currentState.AdvanceTo(currentElapsed);

        _elapsedWorldTime = currentElapsed;
    }

    internal void AdvanceGlobalSimulation(
        long dayNumber,
        TimeSpan elapsedWorldTime)
    {
        if (elapsedWorldTime != _elapsedWorldTime)
        {
            throw new InvalidOperationException(
                "Global simulation time is out of sync with world time.");
        }

        if (dayNumber != _completedDayNumber + 1)
        {
            throw new InvalidOperationException(
                "Global simulation day is out of sequence.");
        }

        // Обрабатываются все сгенерированные системы,
        // а не только посещённые игроком.
        foreach (var systemState in _systemStates.Values)
        {
            systemState.AdvanceGlobalDay(dayNumber);
        }

        _completedDayNumber = dayNumber;
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

        if (string.Equals(
                _currentSystemId,
                system.Id,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Если система посещалась, сохраняем её прежний экземпляр.
        if (!_localSystemStates.TryGetValue(
                system.Id,
                out var localState))
        {
            // При первом посещении генерируем локальные объекты.
            localState = new LocalStarSystemState(
                StarSystemGenerator.Generate(
                    system.Id,
                    system.Seed));

            _localSystemStates.Add(
                system.Id,
                localState);
        }

        // Учитываем прошедшее время, пока система была неактивна.
        localState.AdvanceTo(_elapsedWorldTime);

        _currentSystemId = system.Id;
    }

    public static WorldState Create(WorldStartData startData)
    {
        return new WorldState(
            Guid.NewGuid(),
            startData);
    }

    public static WorldState CreateNew()
    {
        return Create(WorldStartData.CreateNew());
    }
}