using GameProject.World;

namespace GameProject.Simulation;

public sealed class WorldSimulation : IDisposable
{
    private readonly WorldState _world;
    private readonly GameTime _gameTime;

    private bool _disposed;

    public WorldSimulation(
        WorldState world,
        GameTime gameTime)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(gameTime);

        _world = world;
        _gameTime = gameTime;

        _gameTime.Advanced += OnTimeAdvanced;
        _gameTime.DayCompleted += OnDayCompleted;
    }

    private void OnTimeAdvanced(GameTimeAdvanced advanced)
    {
        _world.AdvanceLocalSimulation(
            advanced.PreviousElapsed.Value,
            advanced.CurrentElapsed.Value);
    }

    private void OnDayCompleted(GameDayCompleted completed)
    {
        _world.AdvanceGlobalSimulation(
            completed.DayNumber,
            completed.Elapsed.Value);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _gameTime.Advanced -= OnTimeAdvanced;
        _gameTime.DayCompleted -= OnDayCompleted;
    }
}