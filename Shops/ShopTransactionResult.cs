namespace GameProject.Shops;

public enum ShopTransactionStatus
{
    Success,

    ShopDefinitionMismatch,

    EquipmentNotFoundInShop,
    EquipmentNotFoundInCargoHold,

    EquipmentDefinitionNotFound,
    EquipmentDefinitionMismatch,
    EquipmentLevelNotFound,

    InvalidPrice,
    InsufficientCredits,
    CreditsLimitReached,

    CargoHoldRejected,
    ShopStockRejected
}

public readonly record struct ShopTransactionResult(
    ShopTransactionStatus Status,
    long CreditsTransferred)
{
    public bool Succeeded =>
        Status == ShopTransactionStatus.Success;
}