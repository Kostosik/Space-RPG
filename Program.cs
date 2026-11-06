using System.Text.Json;
using Engine.Hosting.Desktop;
using Engine.Runtime;
using Engine.Serialization.Content;
using Engine.Serialization.UI;
using Engine.UI.Assets;

namespace GameProject;

internal static class Program
{
    private static void Main(string[] args)
    {
        var startupScenePath =
            ResolveStartupScenePath(
                "Space-RPG.vibeproject");

        DesktopGameHost.Run(
        (DesktopGameHostContext context) => new GameMain(
            context.Runtime,
            startupScenePath),
                new DesktopGameHostOptions
                {
                    Title = "Space-RPG",

                    ConfigureContentLoaders = loaders =>
                    {
                        loaders.Register(
                            new BinaryContentLoader<UiAsset>(
                                new UiAssetSerializer(),
                                asset => asset.Extension.Equals(
                                    ".ui",
                                    StringComparison.OrdinalIgnoreCase)));
                    }
                });
    }

    private static string ResolveStartupScenePath(
        string manifestFileName)
    {
        var projectDirectory =
            Path.GetFullPath(
                AppContext.BaseDirectory);

        var manifestPath =
            Path.Combine(
                projectDirectory,
                manifestFileName);

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException(
                "Game project manifest was not found beside the executable.",
                manifestPath);
        }

        using var document =
            JsonDocument.Parse(
                File.ReadAllText(manifestPath));

        var relativeScenePath =
            "Scenes/Main.world";

        if (document.RootElement.TryGetProperty(
                "startupScene",
                out var startupSceneElement) &&
            startupSceneElement.ValueKind ==
                JsonValueKind.String)
        {
            var configuredPath =
                startupSceneElement.GetString();

            if (!string.IsNullOrWhiteSpace(
                    configuredPath))
            {
                relativeScenePath =
                    configuredPath;
            }
        }

        if (Path.IsPathRooted(
                relativeScenePath))
        {
            throw new InvalidDataException(
                "The startupScene path must be relative to the project directory.");
        }

        var fullScenePath =
            Path.GetFullPath(
                Path.Combine(
                    projectDirectory,
                    relativeScenePath));

        var relativePath =
            Path.GetRelativePath(
                projectDirectory,
                fullScenePath);

        if (IsOutsideProjectRoot(
                relativePath))
        {
            throw new InvalidDataException(
                "The startupScene path cannot point outside the project directory.");
        }

        return fullScenePath;
    }

    private static bool IsOutsideProjectRoot(
        string relativePath)
    {
        return Path.IsPathRooted(relativePath)
            || relativePath.Equals(
                "..",
                StringComparison.Ordinal)
            || relativePath.StartsWith(
                $"..{Path.DirectorySeparatorChar}",
                StringComparison.Ordinal)
            || relativePath.StartsWith(
                $"..{Path.AltDirectorySeparatorChar}",
                StringComparison.Ordinal);
    }
}
