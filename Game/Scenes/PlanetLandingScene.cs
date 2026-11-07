using Engine.Core.Math;
using Engine.Core.Time;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Input;
using Engine.Runtime.Scenes;

namespace GameProject.Game.Scenes;

public sealed class PlanetLandingScene : IScene<GameSceneContext>
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

        // Кнопка взлёта.
        if (position.X >= 500 &&
            position.X <= 780 &&
            position.Y >= 590 &&
            position.Y <= 654)
        {
            _context!.TakeOffFromPlanet();

            _requestScene?.Invoke(
                new GameplayScene());
        }
    }

    public void Render(double interpolationAlpha)
    {
        _ = interpolationAlpha;

        // Используем существующий экран посадки.
        _context!.DrawLandingScreen();
    }

    public void Dispose()
    {
        _context = null;
        _requestScene = null;
    }
}