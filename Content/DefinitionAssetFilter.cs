using Engine.Content.Assets;
using Engine.Core.Assets;

namespace GameProject.Content;

public sealed class DefinitionAssetFilter
{
    private readonly string _directoryPrefix;
    private readonly string _fileSuffix;

    public DefinitionAssetFilter(
        string directory,
        string fileSuffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileSuffix);

        var normalizedDirectory =
            new AssetPath(directory).Value;

        if (string.IsNullOrWhiteSpace(normalizedDirectory))
        {
            throw new ArgumentException(
                "Directory cannot be empty.",
                nameof(directory));
        }

        if (!fileSuffix.StartsWith('.'))
        {
            throw new ArgumentException(
                "File suffix must start with a dot.",
                nameof(fileSuffix));
        }

        _directoryPrefix =
            normalizedDirectory + "/";

        _fileSuffix = fileSuffix;
    }

    public bool Matches(ContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        return asset.Path.Value.StartsWith(
                   _directoryPrefix,
                   StringComparison.OrdinalIgnoreCase)
               && asset.Path.Value.EndsWith(
                   _fileSuffix,
                   StringComparison.OrdinalIgnoreCase);
    }
}