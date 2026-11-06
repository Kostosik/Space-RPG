using Engine.Core.Time;

namespace GameProject.Simulation;

public sealed class SimulationState
{
    public GameTime Time { get; } = new();

    public void FixedUpdate(SimulationTime time)
    {
        Time.Advance(time);
    }
}