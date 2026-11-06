using Engine.Core.Math;
using Engine.Core.Time;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Networking.Replication;
using Engine.Runtime;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using GameProject.Game;

namespace GameProject;

internal sealed class GameMain : EngineGame
{
    private readonly string _startupScenePath;
    private GameSession? _session;
    public GameMain(
        EngineRuntime runtime,
        string startupScenePath)
        : base(runtime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            startupScenePath);

        _startupScenePath =
            Path.GetFullPath(
                startupScenePath);
    }

    protected override void ConfigureRuntime()
    {
        if (!File.Exists(_startupScenePath))
        {
            throw new FileNotFoundException(
                "Startup scene was not found.",
                _startupScenePath);
        }

        var serializers =
            new EcsComponentSerializerRegistry();

        serializers.RegisterEngineComponents();
        serializers.RegisterNetworkComponents();
        var saveLoad =
            new WorldSaveLoadService(
                serializers);

        saveLoad.Load(
            _startupScenePath,
            Runtime.World);
    }

    protected override void OnInitialize()
    {
        _session = GameSession.CreateNew();

        Console.WriteLine(
            $"Game session created: {_session.SessionId}");

        Console.WriteLine(
            $"World created: {_session.World.Id}");
    }

    protected override void OnRender(
        double interpolationAlpha)
    {
        Runtime.Services.Graphics.Submit(
            new DrawDebugTextCommand(
                "VibeEngine2 game is running",
                new Vector2(24.0f, 24.0f),
                DebugColor.White,
                2.0f,
                DebugRenderSpace.Screen));
    }

    protected override void OnFixedUpdate(SimulationTime time)
    {
        _session?.Simulation.FixedUpdate(time);
    }

    protected override void OnShutdown()
    {
        if (_session is not null)
        {
            Console.WriteLine(
                $"Game session ended: {_session.SessionId}");

            _session = null;
        }
    }
}
