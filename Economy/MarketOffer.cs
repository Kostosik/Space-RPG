namespace GameProject.Economy;

public sealed class MarketOffer
{
    public MarketOffer(
        string goodId,
        long baseUnitPrice,
        int initialStock,
        int targetStock,
        int stockCapacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(goodId);

        if (baseUnitPrice <= 0 ||
            targetStock <= 0 ||
            initialStock < 0 ||
            initialStock > stockCapacity ||
            stockCapacity < targetStock)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseUnitPrice),
                "Invalid market offer configuration.");
        }

        GoodId = goodId;
        BaseUnitPrice = baseUnitPrice;
        Stock = initialStock;
        TargetStock = targetStock;
        StockCapacity = stockCapacity;
    }

    public string GoodId { get; }

    public long BaseUnitPrice { get; }

    public int Stock { get; private set; }

    public int TargetStock { get; }

    public int StockCapacity { get; }

    public int FreeStock => StockCapacity - Stock;

    // Низкий запас увеличивает цену покупки.
    public long BuyUnitPrice
    {
        get
        {
            var supplyRatio = (double)Stock / TargetStock;

            var demandMultiplier = Math.Clamp(
                1.5 - 0.5 * supplyRatio,
                0.5,
                1.5);

            var price = decimal.Round(
                BaseUnitPrice * (decimal)demandMultiplier,
                0,
                MidpointRounding.AwayFromZero);

            return Math.Max(1L, (long)price);
        }
    }

    // Рынок покупает товар дешевле, чем продаёт.
    public long SellUnitPrice
    {
        get
        {
            var price = decimal.Floor(
                BuyUnitPrice * 0.65m);

            return Math.Max(1L, (long)price);
        }
    }

    internal bool TryRemoveStock(int quantity)
    {
        if (quantity <= 0 || Stock < quantity)
            return false;

        Stock -= quantity;
        return true;
    }

    internal bool TryAddStock(int quantity)
    {
        if (quantity <= 0 || quantity > FreeStock)
            return false;

        Stock += quantity;
        return true;
    }
}