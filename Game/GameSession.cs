using GameProject.Player;
using GameProject.Simulation;
using GameProject.World;

namespace GameProject.Game;

/// <summary>
/// Root object of a single game playthrough.
/// Owns all persistent game state.
/// </summary>
public sealed class GameSession
{
    internal GameSession(
        Guid sessionId,
        PlayerState player,
        WorldState world,
        SimulationState simulation)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Session ID cannot be empty.",
                nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(simulation);

        SessionId = sessionId;
        Player = player;
        World = world;
        Simulation = simulation;
    }

    public Guid SessionId { get; }

    public PlayerState Player { get; }

    public WorldState World { get; }

    public SimulationState Simulation { get; }

    public static GameSession CreateNew()
    {
        return CreateNew(
            WorldStartData.CreateNew());
    }

    public static GameSession CreateNew(
        WorldStartData worldStartData)
    {
        return new GameSession(
            Guid.NewGuid(),
            new PlayerState(),
            WorldState.Create(worldStartData),
            new SimulationState());
    }
}