using Engine.Core.Time;
using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Input;
using Engine.Runtime.Scenes;

namespace GameProject.Game.Scenes;

public sealed class MainMenuScene : IScene<GameSceneContext>
{
    private GameSceneContext? _context;

    private Action<IScene<GameSceneContext>>? _requestScene;

    public void Initialize(
        GameSceneContext context,
        Action<IScene<GameSceneContext>> requestScene)
    {
        _context = context;
        _requestScene = requestScene;
    }

    public void Update(TimeSnapshot time)
    {
        _ = time;

        var pointer = _context?.PointerInput;

        if (pointer is null ||
            !pointer.IsPressed(InputMouseButton.Left))
        {
            return;
        }

        var position = pointer.Position;

        // Кнопка START GAME.
        if (position.X >= 500 &&
            position.X <= 780 &&
            position.Y >= 430 &&
            position.Y <= 500)
        {
            _requestScene?.Invoke(
                new GameplayScene());
        }
    }

    public void Render(double interpolationAlpha)
    {
        _ = interpolationAlpha;

        var graphics = _context!.Runtime.Services.Graphics;

        // Фон.
        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new UiColor(3, 7, 20, 255)));

        // Центральная панель.
        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(340, 180),
                new Vector2(600, 390),
                new UiColor(10, 23, 43, 245)));

        // Заголовок.
        DrawText(
            "SPACE-RPG",
            new Vector2(450, 245),
            new DebugColor(100, 210, 255),
            4.0f);

        DrawText(
            "EXPLORE THE UNKNOWN",
            new Vector2(430, 330),
            DebugColor.White,
            1.7f);

        // Кнопка.
        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(500, 430),
                new Vector2(280, 70),
                new UiColor(20, 100, 145, 255)));

        DrawText(
            "START GAME",
            new Vector2(565, 453),
            DebugColor.White,
            2.0f);

        DrawText(
            "A GALAXY WAITING FOR YOU",
            new Vector2(445, 550),
            new DebugColor(120, 160, 190),
            1.3f);
    }

    private void DrawText(
        string text,
        Vector2 position,
        DebugColor color,
        float scale)
    {
        _context!.Runtime.Services.Graphics.Submit(
            new DrawDebugTextCommand(
                text,
                position,
                color,
                scale,
                DebugRenderSpace.Screen));
    }

    public void Dispose()
    {
        _context = null;
        _requestScene = null;
    }
}