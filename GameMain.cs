using Engine.Core.Math;
using Engine.Graphics.Commands;
using Engine.Graphics.DebugGraphics;
using Engine.Runtime;
using Engine.Serialization.SaveLoad.Ecs;
using Engine.Serialization.SaveLoad.Worlds;
using Engine.Networking.Replication;

namespace GameProject;

internal sealed class GameMain : EngineGame
{
    private readonly string _startupScenePath;

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
        Console.WriteLine(
            $"Startup scene loaded: {_startupScenePath}");

        Console.WriteLine(
            "VibeEngine2 game initialized.");
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

    protected override void OnShutdown()
    {
        Console.WriteLine(
            "VibeEngine2 game stopped.");
    }
}
