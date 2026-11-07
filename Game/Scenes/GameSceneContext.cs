using Engine.Core.Time;
using Engine.Input;
using Engine.Runtime;

namespace GameProject.Game.Scenes;

public sealed class GameSceneContext
{
    public GameSceneContext(
        EngineRuntime runtime,
        IPointerInput? pointerInput,
        Action enterGameplay,
        Action<TimeSnapshot> updateGameplay,
        Action<double> renderGameplay,
        Action drawLandingScreen,
        Action takeOffFromPlanet)
    {
        Runtime = runtime;
        PointerInput = pointerInput;

        EnterGameplay = enterGameplay;
        UpdateGameplay = updateGameplay;
        RenderGameplay = renderGameplay;
        DrawLandingScreen = drawLandingScreen;
        TakeOffFromPlanet = takeOffFromPlanet;
    }

    public EngineRuntime Runtime { get; }

    public IPointerInput? PointerInput { get; }

    internal Action EnterGameplay { get; }

    internal Action<TimeSnapshot> UpdateGameplay { get; }

    internal Action<double> RenderGameplay { get; }

    internal Action DrawLandingScreen { get; }

    internal Action TakeOffFromPlanet { get; }
}