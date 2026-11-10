using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Runtime;
using GameProject.Player;
using GameProject.World;

namespace GameProject.Galaxy;

internal static class GalaxyMapRenderer
{
    private const float MapCenterX = 600.0f;
    private const float MapCenterY = 360.0f;
    private const float MapScale = 0.65f;

    public static void Draw(
        EngineRuntime runtime,
        GalaxyMap galaxy,
        string currentSystemId,
        GalaxyNavigationState navigation,
        string selectedSystemId,
        string message,
        int jumpFuelCost)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(galaxy);
        ArgumentNullException.ThrowIfNull(navigation);

        var graphics = runtime.Services.Graphics;

        // Затемняем игровой мир.
        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new UiColor(5, 9, 22, 235)));

        DrawText(
            runtime,
            "GALACTIC NAVIGATION",
            new Vector2(35, 30),
            new DebugColor(100, 200, 255),
            3.0f);

        DrawText(
            runtime,
            $"FUEL: {navigation.Fuel}/{navigation.FuelCapacity}",
            new Vector2(35, 80),
            DebugColor.White);

        DrawText(
            runtime,
            $"CURRENT SYSTEM: {currentSystemId.ToUpperInvariant()}",
            new Vector2(35, 110),
            DebugColor.Green);

        // Линии гиперпереходов.
        foreach (var system in galaxy.Systems)
        {
            var start = GetScreenPosition(system);

            foreach (var connectionId in system.Connections)
            {
                if (string.CompareOrdinal(
                        system.Id,
                        connectionId) >= 0)
                {
                    continue;
                }

                var connected = galaxy.GetSystem(connectionId);

                if (connected is null)
                    continue;

                var end = GetScreenPosition(connected);

                graphics.Submit(
                    new DrawDebugLineCommand(
                        start,
                        end,
                        new DebugColor(35, 90, 145),
                        DebugRenderSpace.Screen));
            }
        }

        // Звёздные системы.
        foreach (var system in galaxy.Systems)
        {
            var position = GetScreenPosition(system);

            var isCurrent =
                system.Id == currentSystemId;

            var isSelected =
                system.Id == selectedSystemId;

            var color = isCurrent
                ? new DebugColor(80, 255, 130)
                : isSelected
                    ? new DebugColor(255, 210, 80)
                    : new DebugColor(100, 180, 255);

            DrawCircle(
                runtime,
                position,
                isSelected ? 13.0f : 8.0f,
                color);

            DrawText(
                runtime,
                system.Name,
                new Vector2(
                    position.X + 16,
                    position.Y - 7),
                color,
                1.5f);
        }

        // Информация о выбранной системе.
        var selected = galaxy.GetSystem(selectedSystemId);

        if (selected is not null)
        {
            var panelPosition = new Vector2(900, 190);

            graphics.Submit(
                new DrawDebugLineCommand(
                    new Vector2(880, 170),
                    new Vector2(880, 470),
                    new DebugColor(50, 110, 160),
                    DebugRenderSpace.Screen));

            DrawText(
                runtime,
                "SYSTEM INFORMATION",
                panelPosition,
                new DebugColor(100, 200, 255),
                2.0f);

            DrawText(
                runtime,
                selected.Name,
                new Vector2(900, 230),
                DebugColor.White,
                2.0f);

            var isConnected = galaxy.CanTravel(
                currentSystemId,
                selected.Id);

            DrawText(
                runtime,
                isConnected
                    ? "HYPERSPACE: AVAILABLE"
                    : "HYPERSPACE: UNAVAILABLE",
                new Vector2(900, 280),
                isConnected
                    ? DebugColor.Green
                    : DebugColor.Red);

            DrawText(
                runtime,
                $"JUMP COST: {jumpFuelCost} FUEL",
                new Vector2(900, 320),
                DebugColor.White);

            DrawText(
                runtime,
                $"REMAINING: {navigation.Fuel} FUEL",
                new Vector2(900, 350),
                DebugColor.White);

            DrawText(
                runtime,
                selected.Id == currentSystemId
                    ? "CURRENT LOCATION"
                    : isConnected && navigation.Fuel >= jumpFuelCost
                        ? "PRESS ENTER TO JUMP"
                        : "JUMP NOT AVAILABLE",
                new Vector2(900, 410),
                new DebugColor(255, 210, 80));
        }

        DrawText(
            runtime,
            message,
            new Vector2(35, 640),
            new DebugColor(100, 220, 255));

        DrawText(
            runtime,
            "M - CLOSE MAP    LEFT CLICK - SELECT    ENTER - HYPERSPACE JUMP",
            new Vector2(35, 675),
            DebugColor.White,
            1.5f);
    }

    public static GalaxySystem? FindSystemAt(
        GalaxyMap galaxy,
        Vector2 mousePosition)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        foreach (var system in galaxy.Systems)
        {
            var position = GetScreenPosition(system);

            var dx = mousePosition.X - position.X;
            var dy = mousePosition.Y - position.Y;

            if (dx * dx + dy * dy <= 18.0f * 18.0f)
                return system;
        }

        return null;
    }

    private static Vector2 GetScreenPosition(
        GalaxySystem system)
    {
        return new Vector2(
            MapCenterX + system.MapX * MapScale,
            MapCenterY + system.MapY * MapScale);
    }

    private static void DrawText(
        EngineRuntime runtime,
        string text,
        Vector2 position,
        DebugColor color,
        float scale = 2.0f)
    {
        runtime.Services.Graphics.Submit(
            new DrawDebugTextCommand(
                text,
                position,
                color,
                scale,
                DebugRenderSpace.Screen));
    }

    private static void DrawCircle(
        EngineRuntime runtime,
        Vector2 center,
        float radius,
        DebugColor color)
    {
        const int segments = 24;

        var graphics = runtime.Services.Graphics;

        var previous = new Vector2(
            center.X + radius,
            center.Y);

        for (var i = 1; i <= segments; i++)
        {
            var angle =
                Math.PI * 2.0 * i / segments;

            var current = new Vector2(
                center.X + (float)Math.Cos(angle) * radius,
                center.Y + (float)Math.Sin(angle) * radius);

            graphics.Submit(
                new DrawDebugLineCommand(
                    previous,
                    current,
                    color,
                    DebugRenderSpace.Screen));

            previous = current;
        }
    }
}