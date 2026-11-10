using System.Collections.ObjectModel;

namespace GameProject.NPC;

public sealed class NpcEconomyState
{
    private readonly Dictionary<string, int> _cargo =
        new(StringComparer.Ordinal);

    private readonly ReadOnlyDictionary<string, int>
        _readOnlyCargo;

    public NpcEconomyState(long initialCredits = 0)
    {
        if (initialCredits < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCredits));

        Credits = initialCredits;

        _readOnlyCargo =
            new ReadOnlyDictionary<string, int>(_cargo);
    }

    public long Credits { get; private set; }

    public IReadOnlyDictionary<string, int> Cargo =>
        _readOnlyCargo;

    public bool TrySpendCredits(long amount)
    {
        if (amount < 0 || Credits < amount)
            return false;

        Credits -= amount;
        return true;
    }

    public bool TryAddCredits(long amount)
    {
        if (amount < 0 ||
            Credits > long.MaxValue - amount)
        {
            return false;
        }

        Credits += amount;
        return true;
    }

    public int GetCargoQuantity(string itemId)
    {
        return _cargo.TryGetValue(itemId, out var quantity)
            ? quantity
            : 0;
    }

    public bool TryAddCargo(
        string itemId,
        int quantity)
    {
        ValidateItemId(itemId);

        if (quantity <= 0)
            return false;

        var current = GetCargoQuantity(itemId);

        if (current > int.MaxValue - quantity)
            return false;

        _cargo[itemId] = current + quantity;

        return true;
    }

    public bool TryRemoveCargo(
        string itemId,
        int quantity)
    {
        ValidateItemId(itemId);

        if (quantity <= 0)
            return false;

        var current = GetCargoQuantity(itemId);

        if (current < quantity)
            return false;

        if (current == quantity)
            _cargo.Remove(itemId);
        else
            _cargo[itemId] = current - quantity;

        return true;
    }

    private static void ValidateItemId(string itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
    }
}