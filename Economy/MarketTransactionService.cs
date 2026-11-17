using GameProject.Player;

namespace GameProject.Economy;

public sealed class MarketTransactionService
{
    private const decimal SellMultiplier = 0.65m;

    public MarketTransactionResult TryBuy(
        PlayerState player,
        MarketState market,
        string goodId,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(market);

        if (quantity <= 0)
            return Fail(MarketTransactionStatus.InvalidQuantity);

        if (!market.TryGetOffer(goodId, out var offer) ||
            offer is null)
        {
            return Fail(MarketTransactionStatus.OfferNotFound);
        }

        if (offer.Stock < quantity)
            return Fail(MarketTransactionStatus.InsufficientMarketStock);

        if (player.Ship.TradeGoods.FreeCapacity < quantity)
            return Fail(MarketTransactionStatus.InsufficientCargoCapacity);

        var unitPrice = offer.BuyUnitPrice;

        if (!TryCalculateTotal(unitPrice, quantity, out var totalPrice))
            return Fail(MarketTransactionStatus.PriceOverflow);

        if (player.Credits < totalPrice)
            return Fail(MarketTransactionStatus.InsufficientCredits);

        if (!player.TrySpendCredits(totalPrice))
            return Fail(MarketTransactionStatus.InsufficientCredits);

        if (!offer.TryRemoveStock(quantity))
        {
            RestoreCredits(player, totalPrice);

            return Fail(MarketTransactionStatus.InsufficientMarketStock);
        }

        if (!player.Ship.TradeGoods.TryAdd(goodId, quantity))
        {
            RestoreStockAfterPurchase(offer, quantity);
            RestoreCredits(player, totalPrice);

            return Fail(MarketTransactionStatus.InsufficientCargoCapacity);
        }

        return new MarketTransactionResult(
            MarketTransactionStatus.Success,
            unitPrice,
            totalPrice);
    }

    public MarketTransactionResult TrySell(
        PlayerState player,
        MarketState market,
        string goodId,
        int quantity)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(market);

        if (quantity <= 0)
            return Fail(MarketTransactionStatus.InvalidQuantity);

        if (!market.TryGetOffer(goodId, out var offer) ||
            offer is null)
        {
            return Fail(MarketTransactionStatus.OfferNotFound);
        }

        if (player.Ship.TradeGoods.GetQuantity(goodId) < quantity)
            return Fail(MarketTransactionStatus.InsufficientCargo);

        if (offer.FreeStock < quantity)
            return Fail(MarketTransactionStatus.MarketCapacityReached);

        var unitPrice = offer.SellUnitPrice;

        if (!TryCalculateTotal(unitPrice, quantity, out var totalPrice))
            return Fail(MarketTransactionStatus.PriceOverflow);

        if (!player.CanReceiveCredits(totalPrice))
            return Fail(MarketTransactionStatus.CreditsLimitReached);

        if (!player.Ship.TradeGoods.TryRemove(goodId, quantity))
            return Fail(MarketTransactionStatus.InsufficientCargo);

        if (!offer.TryAddStock(quantity))
        {
            RestoreCargo(player, goodId, quantity);

            return Fail(MarketTransactionStatus.MarketCapacityReached);
        }

        if (!player.TryAddCredits(totalPrice))
        {
            RestoreStockAfterSale(offer, quantity);
            RestoreCargo(player, goodId, quantity);

            return Fail(MarketTransactionStatus.CreditsLimitReached);
        }

        return new MarketTransactionResult(
            MarketTransactionStatus.Success,
            unitPrice,
            totalPrice);
    }

    private static bool TryCalculateTotal(
        long unitPrice,
        int quantity,
        out long total)
    {
        total = 0;

        try
        {
            total = checked(unitPrice * quantity);
            return total > 0;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static void RestoreCredits(
        PlayerState player,
        long amount)
    {
        if (!player.TryAddCredits(amount))
        {
            throw new InvalidOperationException(
                "Failed to restore credits.");
        }
    }

    private static void RestoreStockAfterPurchase(
        MarketOffer offer,
        int quantity)
    {
        if (!offer.TryAddStock(quantity))
        {
            throw new InvalidOperationException(
                "Failed to restore market stock after purchase.");
        }
    }

    private static void RestoreStockAfterSale(
        MarketOffer offer,
        int quantity)
    {
        if (!offer.TryRemoveStock(quantity))
        {
            throw new InvalidOperationException(
                "Failed to restore market stock after sale.");
        }
    }

    private static void RestoreCargo(
        PlayerState player,
        string goodId,
        int quantity)
    {
        if (!player.Ship.TradeGoods.TryAdd(goodId, quantity))
        {
            throw new InvalidOperationException(
                "Failed to restore trade goods.");
        }
    }

    private static MarketTransactionResult Fail(
        MarketTransactionStatus status)
    {
        return new MarketTransactionResult(status, 0, 0);
    }
}