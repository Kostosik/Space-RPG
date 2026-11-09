using Engine.Core.Math;
using GameProject.Game;
using GameProject.Player;

namespace GameProject.Galaxy;

internal enum GalaxyJumpResult
{
    Completed,
    TargetNotFound,
    NoConnection,
    NotEnoughFuel,
    JumpFailed
}

internal static class GalaxyJumpService
{
    public static GalaxyJumpResult Execute(
        GameSession session,
        PlayerShipMovementSystem? movementSystem,
        string targetSystemId,
        int fuelCost,
        Vector2 spawnPosition)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSystemId);

        var galaxy = session.World.Galaxy;
        var navigation = session.Player.GalaxyNavigation;

        var target = galaxy.GetSystem(targetSystemId);

        if (target is null)
            return GalaxyJumpResult.TargetNotFound;

        if (!galaxy.CanTravel(
                navigation.CurrentSystemId,
                target.Id))
        {
            return GalaxyJumpResult.NoConnection;
        }

        if (fuelCost <= 0 ||
            navigation.Fuel < fuelCost)
        {
            return GalaxyJumpResult.NotEnoughFuel;
        }

        if (!navigation.TryJump(
                galaxy,
                target.Id,
                fuelCost))
        {
            return GalaxyJumpResult.JumpFailed;
        }

        // Загружаем новую звёздную систему.
        session.World.EnterSystem(target.Id);

        // Обновляем объекты для посадки.
        movementSystem?.SetLandingObjects(
            session.World.StarSystem.Objects);

        // Обновляем навигационные препятствия.
        movementSystem?.SetNavigationObstacles(
            session.World.StarSystem.NavigationObstacles);

        // Перемещаем корабль и очищаем старое состояние.
        movementSystem?.ResetForSystemTransition(
            spawnPosition);

        return GalaxyJumpResult.Completed;
    }
}