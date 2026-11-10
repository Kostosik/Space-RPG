using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Graphics.Fonts;
using Engine.Graphics.Rendering;
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
        FontHandle font,
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
        var mapScaleFactor = GetMapScaleFactor(galaxy);
        // Затемняем игровой мир.
        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new UiColor(5, 9, 22, 235)));

        DrawText(
            runtime, font,
            "GALACTIC NAVIGATION",
            new Vector2(35, 30),
            new DebugColor(100, 200, 255),
            3.0f);

        DrawText(
            runtime, font,
            $"FUEL: {navigation.Fuel}/{navigation.FuelCapacity}",
            new Vector2(35, 80),
            DebugColor.White);

        DrawText(
            runtime, font,
            $"CURRENT SYSTEM: {currentSystemId.ToUpperInvariant()}",
            new Vector2(35, 110),
            DebugColor.Green);

        // Линии гиперпереходов.
        foreach (var system in galaxy.Systems)
        {
            var start = GetScreenPosition(system, mapScaleFactor);

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

                var end = GetScreenPosition(connected, mapScaleFactor);

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
            var position = GetScreenPosition(system, mapScaleFactor);

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
                MathF.Max(
                    3.0f,
                    (isSelected ? 13.0f : 8.0f) * mapScaleFactor),
                color);

            if (galaxy.Systems.Count <= 30 || isCurrent || isSelected)
            {
                DrawText(
                    runtime, font,
                    system.Name,
                    new Vector2(
                        position.X + 16,
                        position.Y - 7),
                    color,
                    1.5f);
            }
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
                runtime, font,
                "SYSTEM INFORMATION",
                panelPosition,
                new DebugColor(100, 200, 255),
                2.0f);

            DrawText(
                runtime, font,
                selected.Name,
                new Vector2(900, 230),
                DebugColor.White,
                2.0f);

            var isConnected = galaxy.CanTravel(
                currentSystemId,
                selected.Id);

            DrawText(
                runtime, font,
                isConnected
                    ? "HYPERSPACE: AVAILABLE"
                    : "HYPERSPACE: UNAVAILABLE",
                new Vector2(900, 280),
                isConnected
                    ? DebugColor.Green
                    : DebugColor.Red);

            DrawText(
                runtime, font,
                $"JUMP COST: {jumpFuelCost} FUEL",
                new Vector2(900, 320),
                DebugColor.White);

            DrawText(
                runtime, font,
                $"REMAINING: {navigation.Fuel} FUEL",
                new Vector2(900, 350),
                DebugColor.White);

            DrawText(
                runtime, font,
                selected.Id == currentSystemId
                    ? "CURRENT LOCATION"
                    : isConnected && navigation.Fuel >= jumpFuelCost
                        ? "PRESS ENTER TO JUMP"
                        : "JUMP NOT AVAILABLE",
                new Vector2(900, 410),
                new DebugColor(255, 210, 80));
        }

        DrawText(
            runtime, font,
            message,
            new Vector2(35, 640),
            new DebugColor(100, 220, 255));

        DrawText(
            runtime,font,
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
        var mapScaleFactor = GetMapScaleFactor(galaxy);
        foreach (var system in galaxy.Systems)
        {
            var position = GetScreenPosition(
                system,
                mapScaleFactor);

            var dx = mousePosition.X - position.X;
            var dy = mousePosition.Y - position.Y;

            var hitRadius = MathF.Max(
                6.0f,
                18.0f * mapScaleFactor);

            if (dx * dx + dy * dy <= hitRadius * hitRadius)
                return system;
        }

        return null;
    }

    private static float GetMapScaleFactor(GalaxyMap galaxy)
    {
        return MathF.Sqrt(
            (float)GalaxyGenerator.DefaultSystemCount /
            Math.Max(1, galaxy.Systems.Count));
    }

    private static Vector2 GetScreenPosition(
        GalaxySystem system,
        float scaleFactor)
    {
        var mapScale = MapScale * scaleFactor;

        return new Vector2(
            MapCenterX + system.MapX * mapScale,
            MapCenterY + system.MapY * mapScale);
    }

    private static void DrawText(
    EngineRuntime runtime,
    FontHandle font,
    string text,
    Vector2 position,
    DebugColor color,
    float scale = 2.0f)
    {
        runtime.Services.Graphics.Submit(
            new DrawUiTextCommand(
                text,
                font,
                position,
                scale * 8.0f,
                new UiColor(
                    color.R,
                    color.G,
                    color.B,
                    color.A),
                RenderLayers.DebugText));
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