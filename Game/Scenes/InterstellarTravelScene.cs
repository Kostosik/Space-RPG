using Engine.Core.Time;
using Engine.Runtime.Scenes;

namespace GameProject.Game.Scenes;

public sealed class InterstellarTravelScene :
    IScene<GameSceneContext>
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
        if (_context is null)
            return;

        // Симуляция продолжается независимо от сцены.
        var completed =
            _context.UpdateInterstellarTravel(time);

        if (completed)
        {
            _requestScene?.Invoke(
                new GameplayScene());
        }
    }

    public void Render(double interpolationAlpha)
    {
        _ = interpolationAlpha;

        _context?.DrawInterstellarTravelScreen();
    }

    public void Dispose()
    {
        _context = null;
        _requestScene = null;
    }
}