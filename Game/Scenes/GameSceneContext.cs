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
        Action<Action> showPlanetLanding,
        Action hidePlanetLanding,
        Action takeOffFromPlanet,
        Action openSettings,
        Action exitGame, Func<TimeSnapshot, bool> updateInterstellarTravel,
        Action drawInterstellarTravelScreen)
    {
        Runtime = runtime;
        PointerInput = pointerInput;

        ExitGame = exitGame;
        EnterGameplay = enterGameplay;
        UpdateGameplay = updateGameplay;
        RenderGameplay = renderGameplay;

        ShowPlanetLanding = showPlanetLanding;
        HidePlanetLanding = hidePlanetLanding;

        TakeOffFromPlanet = takeOffFromPlanet;
        OpenSettings = openSettings;
        UpdateInterstellarTravel = updateInterstellarTravel;
        DrawInterstellarTravelScreen = drawInterstellarTravelScreen;
    }

    internal Func<TimeSnapshot, bool> UpdateInterstellarTravel { get; }

    internal Action DrawInterstellarTravelScreen { get; }

    public EngineRuntime Runtime { get; }

    public IPointerInput? PointerInput { get; }

    internal Action ExitGame { get; }

    internal Action EnterGameplay { get; }

    internal Action<TimeSnapshot> UpdateGameplay { get; }

    internal Action<double> RenderGameplay { get; }

    internal Action<Action> ShowPlanetLanding { get; }

    internal Action HidePlanetLanding { get; }

    internal Action TakeOffFromPlanet { get; }

    internal Action OpenSettings { get; }
}