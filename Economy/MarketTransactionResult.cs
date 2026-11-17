namespace GameProject.Economy;

public enum MarketTransactionStatus
{
    Success,
    OfferNotFound,
    InvalidQuantity,
    InsufficientCredits,
    InsufficientMarketStock,
    InsufficientCargo,
    InsufficientCargoCapacity,
    MarketCapacityReached,
    CreditsLimitReached,
    PriceOverflow
}

public readonly record struct MarketTransactionResult(
    MarketTransactionStatus Status,
    long UnitPrice,
    long TotalPrice)
{
    public bool Succeeded =>
        Status == MarketTransactionStatus.Success;
}