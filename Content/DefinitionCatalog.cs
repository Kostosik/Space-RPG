using Engine.Content;
using Engine.Content.Assets;
using Engine.Core.Assets;

namespace GameProject.Content;

public sealed class DefinitionCatalog<TDefinition> : IDisposable
    where TDefinition : class, IGameDefinition
{
    private readonly IContentManager _content;
    private readonly IContentCatalog _assets;
    private readonly DefinitionAssetFilter _filter;

    private Dictionary<string, Entry> _entries =
        new(StringComparer.Ordinal);

    private bool _disposed;

    public DefinitionCatalog(
        IContentManager content,
        IContentCatalog assets,
        DefinitionAssetFilter filter)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(filter);

        _content = content;
        _assets = assets;
        _filter = filter;
    }

    public int Count => _entries.Count;

    public IReadOnlyList<TDefinition> Definitions =>
        _entries.Values
            .OrderBy(
                entry => entry.Path.Value,
                StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.Definition)
            .ToArray();

    public void LoadAll()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        _assets.Refresh();

        var replacement =
            new Dictionary<string, Entry>(
                StringComparer.Ordinal);

        var loadedPaths =
            new List<AssetPath>();

        try
        {
            var assets = _assets.GetAssets()
                .Where(_filter.Matches)
                .OrderBy(
                    asset => asset.Path.Value,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var asset in assets)
            {
                var definition =
                    _content.Load<TDefinition>(asset.Path);

                loadedPaths.Add(asset.Path);

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    throw new InvalidDataException(
                        $"Definition '{asset.Path}' has an empty ID.");
                }

                if (replacement.TryGetValue(
                        definition.Id,
                        out var existing))
                {
                    throw new InvalidDataException(
                        $"Duplicate definition ID '{definition.Id}'. " +
                        $"Resources: '{existing.Path}' and '{asset.Path}'.");
                }

                replacement.Add(
                    definition.Id,
                    new Entry(
                        definition,
                        asset.Path));
            }
        }
        catch
        {
            ReleaseResources(loadedPaths);

            throw;
        }

        var previous = _entries;

        _entries = replacement;

        ReleaseResources(
            previous.Values.Select(entry => entry.Path));
    }

    public bool TryGet(
        string id,
        out TDefinition? definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (_entries.TryGetValue(id, out var entry))
        {
            definition = entry.Definition;
            return true;
        }

        definition = null;
        return false;
    }

    public TDefinition GetRequired(string id)
    {
        if (!TryGet(id, out var definition))
        {
            throw new KeyNotFoundException(
                $"Definition '{id}' was not found.");
        }

        return definition!;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ReleaseResources(
            _entries.Values.Select(entry => entry.Path));

        _entries.Clear();
    }

    private void ReleaseResources(
        IEnumerable<AssetPath> paths)
    {
        foreach (var path in paths)
        {
            _content.Unload<TDefinition>(path);
        }
    }

    private sealed record Entry(
        TDefinition Definition,
        AssetPath Path);
}