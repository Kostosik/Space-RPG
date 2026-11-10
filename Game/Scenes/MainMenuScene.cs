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



        // START GAME.
        if (position.X >= 500 &&
            position.X <= 780 &&
            position.Y >= 390 &&
            position.Y <= 460)
        {
            _requestScene?.Invoke(new GameplayScene());
            return;
        }
        // SETTINGS.
        if (position.X >= 500 &&
            position.X <= 780 &&
            position.Y >= 475 &&
            position.Y <= 545)
        {
            _context?.OpenSettings();
            return;
        }

        // EXIT GAME.
        if (position.X >= 500 &&
            position.X <= 780 &&
            position.Y >= 560 &&
            position.Y <= 630)
        {
            _context?.ExitGame();
        }
    }

    public void Render(double interpolationAlpha)
    {
        _ = interpolationAlpha;

        var graphics = _context!.Runtime.Services.Graphics;

        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(0, 0),
                new Vector2(1280, 720),
                new UiColor(3, 7, 20, 255)));

        graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(340, 150),
                new Vector2(600, 500),
                new UiColor(10, 23, 43, 245)));



        DrawText(
            "SPACE-RPG",
            new Vector2(450, 220),
            new DebugColor(100, 210, 255),
            4.0f);

        DrawText(
            "EXPLORE THE UNKNOWN",
            new Vector2(430, 305),
            DebugColor.White,
            1.7f);

        DrawButton(
            "START GAME",
            390,
            new UiColor(20, 100, 145, 255));

        DrawButton(
            "SETTINGS",
            475,
            new UiColor(35, 75, 105, 255));

        DrawButton(
            "EXIT GAME",
            560,
            new UiColor(110, 45, 55, 255));

        DrawText(
            "A GALAXY WAITING FOR YOU",
            new Vector2(445, 635),
            new DebugColor(120, 160, 190),
            1.3f);
    }
    private void DrawButton(
        string text,
        float y,
        UiColor background)
    {
        _context!.Runtime.Services.Graphics.Submit(
            new DrawUiRectangleCommand(
                new Vector2(500, y),
                new Vector2(280, 70),
                background));

        DrawText(
            text,
            new Vector2(565, y + 23),
            DebugColor.White,
            2.0f);
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