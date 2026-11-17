using System.Collections.ObjectModel;

namespace GameProject.Economy;

public sealed class TradeGoodsHold
{
    private readonly Dictionary<string, int> _quantities =
        new(StringComparer.Ordinal);

    private readonly ReadOnlyDictionary<string, int> _readOnlyQuantities;

    private int _capacity;

    public TradeGoodsHold()
    {
        _readOnlyQuantities =
            new ReadOnlyDictionary<string, int>(_quantities);
    }

    public int Capacity => _capacity;

    public long UsedCapacity =>
        _quantities.Values.Sum(x => (long)x);

    public int FreeCapacity =>
        (int)Math.Max(0L, _capacity - UsedCapacity);

    public bool IsOverCapacity =>
        UsedCapacity > _capacity;

    public IReadOnlyDictionary<string, int> Quantities =>
        _readOnlyQuantities;

    public void SetCapacity(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity));
        }

        // При уменьшении вместимости существующий груз не удаляется.
        _capacity = capacity;
    }

    public int GetQuantity(string goodId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goodId);

        return _quantities.TryGetValue(goodId, out var quantity)
            ? quantity
            : 0;
    }

    public bool TryAdd(string goodId, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goodId);

        if (!string.Equals(
                goodId,
                goodId.Trim(),
                StringComparison.Ordinal))
        {
            return false;
        }

        if (quantity <= 0 ||
            (long)quantity + UsedCapacity > Capacity)
        {
            return false;
        }

        var current = GetQuantity(goodId);

        if (current > int.MaxValue - quantity)
            return false;

        _quantities[goodId] = current + quantity;

        return true;
    }

    public bool TryRemove(string goodId, int quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goodId);

        if (quantity <= 0 ||
            !_quantities.TryGetValue(goodId, out var current) ||
            current < quantity)
        {
            return false;
        }

        if (current == quantity)
            _quantities.Remove(goodId);
        else
            _quantities[goodId] = current - quantity;

        return true;
    }
}