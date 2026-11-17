using System.Collections.ObjectModel;

namespace GameProject.Economy;

public sealed class MarketState
{
    private readonly Dictionary<string, MarketOffer> _offers =
        new(StringComparer.Ordinal);

    private readonly ReadOnlyDictionary<string, MarketOffer> _readOnlyOffers;

    public MarketState()
    {
        _readOnlyOffers =
            new ReadOnlyDictionary<string, MarketOffer>(_offers);
    }

    public IReadOnlyDictionary<string, MarketOffer> Offers =>
        _readOnlyOffers;

    public bool TryGetOffer(
        string goodId,
        out MarketOffer? offer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goodId);

        return _offers.TryGetValue(goodId, out offer);
    }

    internal void AddOffer(MarketOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (!_offers.TryAdd(offer.GoodId, offer))
        {
            throw new InvalidOperationException(
                $"Market already contains '{offer.GoodId}'.");
        }
    }
}