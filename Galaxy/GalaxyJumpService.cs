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
    JumpFailed
}

internal static class GalaxyJumpService
{
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

    public static GalaxyJumpResult Execute(
        GameSession session,
        PlayerShipMovementSystem? movementSystem,
        string targetSystemId,
        int fuelCost)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSystemId);

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

        if (!navigation.TryConsumeFuel(fuelCost))
        {
            return GalaxyJumpResult.NotEnoughFuel;
        }

        // Направление движения между звёздными системами.
        var dx = (double)target.MapX - current.MapX;
        var dy = (double)target.MapY - current.MapY;

        var length = Math.Sqrt(dx * dx + dy * dy);

        if (!double.IsFinite(length) || length <= 0.000001)
            return GalaxyJumpResult.JumpFailed;

        if (!navigation.TryConsumeFuel(fuelCost))
        {
            return GalaxyJumpResult.NotEnoughFuel;
        }

        // Загружаем новую звёздную систему.
        session.World.EnterSystem(target.Id);

        var starSystem = session.World.StarSystem;

        // Появляемся со стороны предыдущей системы.
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

        // Корабль смотрит внутрь новой системы.
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