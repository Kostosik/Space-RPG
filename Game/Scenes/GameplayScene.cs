using Engine.Core.Time;
using Engine.Runtime.Scenes;

namespace GameProject.Game.Scenes;

public sealed class GameplayScene : IScene<GameSceneContext>
{
    private GameSceneContext? _context;

    public void Initialize(
        GameSceneContext context,
        Action<IScene<GameSceneContext>> requestScene)
    {
        _ = requestScene;

        _context = context;

        // Разрешаем управление кораблём.
        _context.EnterGameplay();
    }

    public void Update(TimeSnapshot time)
    {
        _context?.UpdateGameplay(time);
    }

    public void Render(double interpolationAlpha)
    {
        _context?.RenderGameplay(interpolationAlpha);
    }

    public void Dispose()
    {
        _context = null;
    }
}