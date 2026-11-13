using Engine.Core.Math;
using GameProject.Game;
using GameProject.Player;
using GameProject.World;

namespace GameProject.Galaxy;

internal enum GalaxyJumpResult
{
    Completed,
    RouteReady,
    TargetNotFound,
    NoConnection,
    NotEnoughFuel,
    TravelStarted,
    JumpFailed
}

internal sealed record GalaxyJumpTravel(
    string SourceSystemId,
    string DestinationSystemId,
    TimeSpan DepartureTime,
    TimeSpan ArrivalTime);

internal static class GalaxyJumpService
{
    internal const int TravelDurationDays = 2;
    public static GalaxyJumpResult TryGetExitPosition(
        GameSession session,
        string targetSystemId,
        int fuelCost,
        out Vector2 exitPosition)
    {
        ArgumentNullException.ThrowIfNull(session);

        exitPosition = default;

        var galaxy = session.World.Galaxy;
        var navigation = session.Player.GalaxyNavigation;

        var target = galaxy.GetSystem(targetSystemId);

        if (target is null)
            return GalaxyJumpResult.TargetNotFound;

        if (!galaxy.CanTravel(
                session.World.CurrentSystemId,
                target.Id))
        {
            return GalaxyJumpResult.NoConnection;
        }

        if (fuelCost <= 0 ||
            navigation.Fuel < fuelCost)
        {
            return GalaxyJumpResult.NotEnoughFuel;
        }

        var current = galaxy.GetSystem(
            session.World.CurrentSystemId);

        if (current is null)
            return GalaxyJumpResult.JumpFailed;

        var dx = (double)target.MapX - current.MapX;
        var dy = (double)target.MapY - current.MapY;

        var length = Math.Sqrt(dx * dx + dy * dy);

        if (!double.IsFinite(length) || length <= 0.000001)
            return GalaxyJumpResult.JumpFailed;

        var starSystem = session.World.StarSystem;

        var exitRadius =
            starSystem.JumpBoundaryRadius -
            StarSystem.DefaultJumpBoundaryClearance;

        if (!double.IsFinite(exitRadius) || exitRadius <= 0)
            return GalaxyJumpResult.JumpFailed;

        exitPosition = new Vector2(
            (float)(starSystem.Star.X + dx / length * exitRadius),
            (float)(starSystem.Star.Y + dy / length * exitRadius));

        if (!float.IsFinite(exitPosition.X) ||
            !float.IsFinite(exitPosition.Y))
        {
            exitPosition = default;
            return GalaxyJumpResult.JumpFailed;
        }

        return GalaxyJumpResult.RouteReady;
    }

    public static GalaxyJumpResult BeginTravel(
    GameSession session,
    string targetSystemId,
    int fuelCost,
    out GalaxyJumpTravel? travel)
    {
        ArgumentNullException.ThrowIfNull(session);

        travel = null;

        var galaxy = session.World.Galaxy;
        var navigation = session.Player.GalaxyNavigation;

        var target = galaxy.GetSystem(targetSystemId);

        if (target is null)
            return GalaxyJumpResult.TargetNotFound;

        var current = galaxy.GetSystem(
            session.World.CurrentSystemId);

        if (current is null)
            return GalaxyJumpResult.JumpFailed;

        if (!galaxy.CanTravel(current.Id, target.Id))
            return GalaxyJumpResult.NoConnection;

        var dx = (double)target.MapX - current.MapX;
        var dy = (double)target.MapY - current.MapY;

        var length = Math.Sqrt(dx * dx + dy * dy);

        if (!double.IsFinite(length) || length <= 0.000001)
            return GalaxyJumpResult.JumpFailed;

        // Топливо списывается именно при отправлении.
        if (!navigation.TryConsumeFuel(fuelCost))
            return GalaxyJumpResult.NotEnoughFuel;

        var departureTime = session.Simulation.Time.Elapsed.Value;

        var arrivalTime = departureTime.Add(
            TimeSpan.FromDays(TravelDurationDays));

        travel = new GalaxyJumpTravel(
            current.Id,
            target.Id,
            departureTime,
            arrivalTime);

        return GalaxyJumpResult.TravelStarted;
    }

    public static GalaxyJumpResult CompleteTravel(
    GameSession session,
    PlayerShipMovementSystem? movementSystem,
    GalaxyJumpTravel travel)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(travel);

        var galaxy = session.World.Galaxy;

        if (!string.Equals(
                session.World.CurrentSystemId,
                travel.SourceSystemId,
                StringComparison.OrdinalIgnoreCase))
        {
            return GalaxyJumpResult.JumpFailed;
        }

        var current = galaxy.GetSystem(travel.SourceSystemId);
        var target = galaxy.GetSystem(travel.DestinationSystemId);

        if (current is null || target is null ||
            !galaxy.CanTravel(current.Id, target.Id))
        {
            return GalaxyJumpResult.JumpFailed;
        }

        var dx = (double)target.MapX - current.MapX;
        var dy = (double)target.MapY - current.MapY;

        var length = Math.Sqrt(dx * dx + dy * dy);

        if (!double.IsFinite(length) || length <= 0.000001)
            return GalaxyJumpResult.JumpFailed;

        // Только теперь переключаем активную систему.
        session.World.EnterSystem(target.Id);

        var starSystem = session.World.StarSystem;

        var availableOffset =
            starSystem.JumpBoundaryRadius -
            starSystem.Star.NavigationRadius -
            1.0;

        var arrivalOffset = Math.Min(
            StarSystem.DefaultArrivalBoundaryOffset,
            availableOffset);

        var arrivalRadius =
            starSystem.JumpBoundaryRadius - arrivalOffset;

        var spawnPosition = new Vector2(
            (float)(starSystem.Star.X - dx / length * arrivalRadius),
            (float)(starSystem.Star.Y - dy / length * arrivalRadius));

        var entryRotation = MathF.Atan2(
            (float)dy,
            (float)dx);

        movementSystem?.SetStarSystem(starSystem);

        movementSystem?.ResetForSystemTransition(
            spawnPosition,
            entryRotation);

        return GalaxyJumpResult.Completed;
    }
}