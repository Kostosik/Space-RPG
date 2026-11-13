using Engine.Core.Time;
using Engine.Runtime.Scenes;

namespace GameProject.Game.Scenes;

public sealed class PlanetLandingScene : IScene<GameSceneContext>
{
    private GameSceneContext? _context;

    public void Initialize(
        GameSceneContext context,
        Action<IScene<GameSceneContext>> requestScene)
    {
        _context = context;

        context.ShowPlanetLanding(() =>
        {
            context.TakeOffFromPlanet();

            requestScene(
                new GameplayScene());
        });
    }

    public void Update(TimeSnapshot time)
    {
        _ = time;
    }

    public void Render(double interpolationAlpha)
    {
        _ = interpolationAlpha;

        // Отрисовка выполняется через UiSystem.
    }

    public void Dispose()
    {
        _context?.HidePlanetLanding();

        _context = null;
    }
}