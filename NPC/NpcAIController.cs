using Engine.AI.Components;
using Engine.AI.Core;
using Engine.AI.DecisionMaking.Utility;
using Engine.AI.Scheduling;
using Engine.AI.Systems;
using Engine.Core.Systems;
using Engine.Core.Time;
using Engine.ECS.Entities;
using GameProject.Simulation;
using GameProject.World;

namespace GameProject.NPC;

internal sealed class NpcAIController : IDisposable
{
    private readonly Engine.ECS.World _ecsWorld;
    private readonly WorldState _world;
    private readonly NpcMovementSimulation _movement;
    private readonly AISystem _aiSystem;

    // Одна сущность AI соответствует одному постоянному NPC.
    private readonly Dictionary<EntityId, NpcState> _agents = new();

    // UtilityAIBehavior хранит runtime-состояние отдельно
    // для каждого агента через AIAgentState.
    private readonly UtilityAIBehavior _behavior;

    private bool _disposed;

    public NpcAIController(
        Engine.ECS.World ecsWorld,
        WorldState world,
        NpcMovementSimulation movement)
    {
        ArgumentNullException.ThrowIfNull(ecsWorld);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(movement);

        _ecsWorld = ecsWorld;
        _world = world;
        _movement = movement;

        _aiSystem = new AISystem(
            _ecsWorld,
            new AISchedulerOptions
            {
                MaxAgentsPerUpdate = Math.Max(
                    64,
                    _world.Npcs.Count),

                StaggerInitialUpdates = false
            });

        _behavior = CreateBehavior();

        foreach (var npc in _world.Npcs.All.OrderBy(x => x.Id))
        {
            var entity = _ecsWorld.CreateEntity();

            _ecsWorld.Add(
                entity,
                new AIComponent());

            _ecsWorld.Add(
                entity,
                new AIUpdateSettings(
                    AIUpdateGroup.EveryUpdate));

            _agents.Add(entity, npc);

            _aiSystem.SetBehavior(
                entity,
                _behavior);
        }

        // Первоначальный выбор целей при создании мира.
        // Это начальная инициализация, а не продвижение времени.
        Update(_world.ElapsedWorldTime, 0);
    }

    private UtilityAIBehavior CreateBehavior()
    {
        var behavior = new UtilityAIBehavior
        {
            MinimumSwitchScoreDelta = 0.05f
        };

        var visitPlanet = new UtilityAction(
            "Visit planet",
            (in AIContext context) =>
            {
                if (TryGetNpc(context.Entity, out var npc))
                {
                    _movement.TryAssignPlanetTarget(npc.Id);
                }
            });

        visitPlanet.AddConsideration(
            new DelegateUtilityConsideration(
                "Planet preference",
                ScorePlanetVisit));

        var travelSystem = new UtilityAction(
            "Travel to system",
            (in AIContext context) =>
            {
                if (TryGetNpc(context.Entity, out var npc))
                {
                    _movement.TryAssignSystemTarget(npc.Id);
                }
            });

        travelSystem.AddConsideration(
            new DelegateUtilityConsideration(
                "System travel preference",
                ScoreSystemTravel));

        behavior
            .AddAction(visitPlanet)
            .AddAction(travelSystem);

        return behavior;
    }

    private float ScorePlanetVisit(in AIContext context)
    {
        if (!TryGetNpc(context.Entity, out var npc))
            return 0f;

        if (!CanChooseNewGoal(npc))
            return 0f;

        if (_world.Galaxy.GetSystem(npc.Location.SystemId) is null)
            return 0f;

        var curiosity = GetTrait(
            npc,
            "curiosity",
            0.5f);

        var caution = GetTrait(
            npc,
            "caution",
            0.5f);

        // Осторожные NPC чаще остаются внутри системы.
        // Любознательные немного чаще предпочитают путешествия.
        return Math.Clamp(
            0.72f + caution * 0.15f - curiosity * 0.12f,
            0.01f,
            1.0f);
    }

    private float ScoreSystemTravel(in AIContext context)
    {
        if (!TryGetNpc(context.Entity, out var npc))
            return 0f;

        if (!CanChooseNewGoal(npc))
            return 0f;

        // Для тестового поведения NPC сначала посещает
        // несколько планет, а затем рассматривает переход.
        if (npc.Movement.PlanetStopsSinceLastJump < 2)
            return 0f;

        var currentSystem = _world.Galaxy.GetSystem(
            npc.Location.SystemId);

        if (currentSystem is null ||
            currentSystem.Connections.Count == 0)
        {
            return 0f;
        }

        var curiosity = GetTrait(
            npc,
            "curiosity",
            0.5f);

        var caution = GetTrait(
            npc,
            "caution",
            0.5f);

        var stopProgress = Math.Clamp(
            npc.Movement.PlanetStopsSinceLastJump / 4.0f,
            0.0f,
            1.0f);

        return Math.Clamp(
            0.35f +
            curiosity * 0.35f +
            (1.0f - caution) * 0.15f +
            (float)stopProgress * 0.15f,
            0.01f,
            1.0f);
    }

    private static bool CanChooseNewGoal(NpcState npc)
    {
        // Нельзя менять цель во время перелёта.
        // Новая цель выбирается, только если корабль прибыл
        // либо ещё не имеет назначения.
        return !npc.Movement.HasTarget ||
               npc.Movement.IsArrived;
    }

    private static float GetTrait(
        NpcState npc,
        string traitId,
        float fallback)
    {
        return npc.Personality.TryGetTrait(
            traitId,
            out var value)
                ? value
                : fallback;
    }

    private bool TryGetNpc(
        EntityId entity,
        out NpcState npc)
    {
        if (_agents.TryGetValue(entity, out var found))
        {
            npc = found;
            return true;
        }

        npc = null!;
        return false;
    }

    // Вызывается только на границе игровых суток.
    public void Update(
        TimeSpan elapsed,
        ulong tick)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        var snapshot = new TimeSnapshot(
            Duration.Zero,
            new Duration(elapsed),
            new Tick(tick));

        _aiSystem.Update(
            new SystemContext(snapshot));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var entity in _agents.Keys.ToArray())
        {
            _aiSystem.RemoveBehavior(entity);

            if (_ecsWorld.Exists(entity))
            {
                _ecsWorld.DestroyEntity(entity);
            }
        }

        _agents.Clear();

        _aiSystem.Dispose();

        _disposed = true;
    }
}