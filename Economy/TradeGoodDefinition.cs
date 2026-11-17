using GameProject.Content;

namespace GameProject.Economy;

public sealed class TradeGoodDefinition : IGameDefinition
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public long BasePrice { get; init; }
}