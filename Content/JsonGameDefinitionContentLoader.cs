using System.Text.Json;
using Engine.Content.Assets;
using Engine.Content.Loading;
using Engine.Core.Assets;

namespace GameProject.Content;

public sealed class JsonGameDefinitionContentLoader<TDefinition> :
    IContentLoader<TDefinition>
    where TDefinition : class, IGameDefinition
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly DefinitionAssetFilter _filter;

    public JsonGameDefinitionContentLoader(
        DefinitionAssetFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _filter = filter;
    }

    public Type AssetType =>
        typeof(TDefinition);

    public bool CanLoad(ContentAsset asset)
    {
        return _filter.Matches(asset);
    }

    public TDefinition Load(
        AssetPath path,
        IContentLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Deserialize(
            path,
            context.ReadBytes(path));
    }

    public async ValueTask<TDefinition> LoadAsync(
        AssetPath path,
        IContentLoadContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var data = await context.ReadBytesAsync(
            path,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        return Deserialize(path, data);
    }

    object IContentLoader.Load(
        AssetPath path,
        IContentLoadContext context)
    {
        return Load(path, context);
    }

    private static TDefinition Deserialize(
        AssetPath path,
        ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new InvalidDataException(
                $"Game definition '{path}' is empty.");
        }

        TDefinition? definition;

        try
        {
            definition = JsonSerializer.Deserialize<TDefinition>(
                data.Span,
                Options);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Game definition '{path}' contains invalid JSON.",
                exception);
        }

        if (definition is null)
        {
            throw new InvalidDataException(
                $"Game definition '{path}' is null.");
        }

        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new InvalidDataException(
                $"Game definition '{path}' has an empty ID.");
        }

        if (!string.Equals(
                definition.Id,
                definition.Id.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Game definition '{path}' has whitespace around its ID.");
        }

        return definition;
    }
}