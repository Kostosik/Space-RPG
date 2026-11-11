using GameProject.NPC;
using GameProject.Ships;
using GameProject.World;

namespace GameProject.Simulation;

public sealed class WorldSimulation : IDisposable
{
    private readonly WorldState _world;
    private readonly GameTime _gameTime;
    private readonly NpcMovementSimulation _npcMovement;
    private readonly NpcAIController _npcAI;
    private bool _disposed;

    public WorldSimulation(
    WorldState world,
    GameTime gameTime,
    IReadOnlyDictionary<string, ShipDefinition> shipDefinitions,
    Engine.ECS.World ecsWorld)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(gameTime);
        ArgumentNullException.ThrowIfNull(shipDefinitions);
        ArgumentNullException.ThrowIfNull(ecsWorld);

        _world = world;
        _gameTime = gameTime;

        _npcMovement = new NpcMovementSimulation(
            world,
            shipDefinitions);

        _npcAI = new NpcAIController(
            ecsWorld,
            world,
            _npcMovement);

        _gameTime.Advanced += OnTimeAdvanced;
        _gameTime.DayCompleted += OnDayCompleted;
    }

    private void OnTimeAdvanced(GameTimeAdvanced advanced)
    {
        _world.AdvanceLocalSimulation(
            advanced.PreviousElapsed.Value,
            advanced.CurrentElapsed.Value);

        _npcMovement.Advance(
            advanced.Delta.Value,
            advanced.CurrentElapsed.Value);
    }

    private void OnDayCompleted(GameDayCompleted completed)
    {
        _world.AdvanceGlobalSimulation(
            completed.DayNumber,
            completed.Elapsed.Value);

        _npcAI.Update(
            completed.Elapsed.Value,
            (ulong)completed.DayNumber);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _npcAI.Dispose();
        _gameTime.Advanced -= OnTimeAdvanced;
        _gameTime.DayCompleted -= OnDayCompleted;
    }
}