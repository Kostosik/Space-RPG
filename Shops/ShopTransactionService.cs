using GameProject.Player;
using GameProject.Ships;

namespace GameProject.Shops;

/// <summary>
/// Performs equipment transactions without depending on UI or game scenes.
/// </summary>
public sealed class ShopTransactionService
{
    public ShopTransactionResult TryBuy(
        PlayerState player,
        ShopDefinition shopDefinition,
        ShopStock shopStock,
        Guid equipmentInstanceId,
        IReadOnlyDictionary<string, EquipmentDefinition> equipmentDefinitions)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(shopDefinition);
        ArgumentNullException.ThrowIfNull(shopStock);
        ArgumentNullException.ThrowIfNull(equipmentDefinitions);

        if (!IsMatchingShop(shopDefinition, shopStock))
        {
            return Result(ShopTransactionStatus.ShopDefinitionMismatch);
        }

        if (!HasValidPriceMultipliers(shopDefinition))
        {
            return Result(ShopTransactionStatus.InvalidPrice);
        }

        if (!shopStock.Items.TryGet(
                equipmentInstanceId,
                out var equipment) ||
            equipment is null)
        {
            return Result(ShopTransactionStatus.EquipmentNotFoundInShop);
        }

        var basePriceResult = ResolveBasePrice(
            equipment,
            equipmentDefinitions,
            out var basePrice);

        if (basePriceResult != ShopTransactionStatus.Success)
            return Result(basePriceResult);

        if (!TryCalculatePrice(
                basePrice,
                shopDefinition.PurchasePriceMultiplier,
                roundUp: true,
                out var price))
        {
            return Result(ShopTransactionStatus.InvalidPrice);
        }

        if (player.Credits < price)
            return Result(ShopTransactionStatus.InsufficientCredits);

        // Извлекаем конкретный экземпляр из магазина.
        if (!shopStock.Items.TryRemove(
                equipmentInstanceId,
                out var removedEquipment) ||
            removedEquipment is null)
        {
            return Result(ShopTransactionStatus.EquipmentNotFoundInShop);
        }

        // Перемещаем тот же экземпляр в трюм корабля.
        if (!player.Ship.CargoHold.TryAdd(removedEquipment))
        {
            RestoreToShop(shopStock, removedEquipment);

            return Result(ShopTransactionStatus.CargoHoldRejected);
        }

        // Списываем деньги только после успешного перемещения.
        if (!player.TrySpendCredits(price))
        {
            RollbackPurchase(player, shopStock, removedEquipment);

            return Result(ShopTransactionStatus.InsufficientCredits);
        }

        return Result(
            ShopTransactionStatus.Success,
            price);
    }

    public ShopTransactionResult TrySell(
        PlayerState player,
        ShopDefinition shopDefinition,
        ShopStock shopStock,
        Guid equipmentInstanceId,
        IReadOnlyDictionary<string, EquipmentDefinition> equipmentDefinitions)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(shopDefinition);
        ArgumentNullException.ThrowIfNull(shopStock);
        ArgumentNullException.ThrowIfNull(equipmentDefinitions);

        if (!IsMatchingShop(shopDefinition, shopStock))
        {
            return Result(ShopTransactionStatus.ShopDefinitionMismatch);
        }

        if (!HasValidPriceMultipliers(shopDefinition))
        {
            return Result(ShopTransactionStatus.InvalidPrice);
        }

        if (!player.Ship.CargoHold.TryGet(
                equipmentInstanceId,
                out var equipment) ||
            equipment is null)
        {
            return Result(ShopTransactionStatus.EquipmentNotFoundInCargoHold);
        }

        var basePriceResult = ResolveBasePrice(
            equipment,
            equipmentDefinitions,
            out var basePrice);

        if (basePriceResult != ShopTransactionStatus.Success)
            return Result(basePriceResult);

        if (!TryCalculatePrice(
                basePrice,
                shopDefinition.SellPriceMultiplier,
                roundUp: false,
                out var price))
        {
            return Result(ShopTransactionStatus.InvalidPrice);
        }

        if (!player.CanReceiveCredits(price))
            return Result(ShopTransactionStatus.CreditsLimitReached);

        // Убираем предмет из трюма.
        if (!player.Ship.CargoHold.TryRemove(
                equipmentInstanceId,
                out var removedEquipment) ||
            removedEquipment is null)
        {
            return Result(ShopTransactionStatus.EquipmentNotFoundInCargoHold);
        }

        // Магазин принимает тот же экземпляр.
        if (!shopStock.Items.TryAdd(removedEquipment))
        {
            RestoreToCargoHold(player, removedEquipment);

            return Result(ShopTransactionStatus.ShopStockRejected);
        }

        // Начисляем деньги после успешного перемещения.
        if (!player.TryAddCredits(price))
        {
            RollbackSale(player, shopStock, removedEquipment);

            return Result(ShopTransactionStatus.CreditsLimitReached);
        }

        return Result(
            ShopTransactionStatus.Success,
            price);
    }

    private static bool IsMatchingShop(
        ShopDefinition shopDefinition,
        ShopStock shopStock)
    {
        return !string.IsNullOrWhiteSpace(shopDefinition.Id) &&
               string.Equals(
                   shopDefinition.Id,
                   shopStock.ShopDefinitionId,
                   StringComparison.Ordinal);
    }

    private static bool HasValidPriceMultipliers(
        ShopDefinition shopDefinition)
    {
        var purchase = shopDefinition.PurchasePriceMultiplier;
        var sell = shopDefinition.SellPriceMultiplier;

        return double.IsFinite(purchase) &&
               purchase > 0 &&
               double.IsFinite(sell) &&
               sell >= 0 &&
               sell <= purchase;
    }

    private static ShopTransactionStatus ResolveBasePrice(
        EquipmentInstance equipment,
        IReadOnlyDictionary<string, EquipmentDefinition> definitions,
        out long basePrice)
    {
        basePrice = 0;

        if (!definitions.TryGetValue(
                equipment.DefinitionId,
                out var definition) ||
            definition is null)
        {
            return ShopTransactionStatus.EquipmentDefinitionNotFound;
        }

        if (!string.Equals(
                definition.Id,
                equipment.DefinitionId,
                StringComparison.Ordinal))
        {
            return ShopTransactionStatus.EquipmentDefinitionMismatch;
        }

        if (definition.Levels is null)
            return ShopTransactionStatus.EquipmentLevelNotFound;

        var level = definition.Levels.FirstOrDefault(
            candidate =>
                candidate is not null &&
                candidate.Level == equipment.Level);

        if (level is null)
            return ShopTransactionStatus.EquipmentLevelNotFound;

        if (level.BasePrice <= 0)
            return ShopTransactionStatus.InvalidPrice;

        basePrice = level.BasePrice;

        return ShopTransactionStatus.Success;
    }

    private static bool TryCalculatePrice(
        long basePrice,
        double multiplier,
        bool roundUp,
        out long price)
    {
        price = 0;

        if (basePrice <= 0 ||
            !double.IsFinite(multiplier) ||
            multiplier < 0)
        {
            return false;
        }

        try
        {
            var rawPrice = checked(
                (decimal)basePrice * (decimal)multiplier);

            var roundedPrice = roundUp
                ? decimal.Ceiling(rawPrice)
                : decimal.Floor(rawPrice);

            if (roundedPrice < 0 ||
                roundedPrice > long.MaxValue)
            {
                return false;
            }

            price = (long)roundedPrice;

            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static void RestoreToShop(
        ShopStock shopStock,
        EquipmentInstance equipment)
    {
        if (!shopStock.Items.TryAdd(equipment))
        {
            throw new InvalidOperationException(
                "Failed to restore equipment to shop stock.");
        }
    }

    private static void RestoreToCargoHold(
        PlayerState player,
        EquipmentInstance equipment)
    {
        if (!player.Ship.CargoHold.TryAdd(equipment))
        {
            throw new InvalidOperationException(
                "Failed to restore equipment to cargo hold.");
        }
    }

    private static void RollbackPurchase(
        PlayerState player,
        ShopStock shopStock,
        EquipmentInstance equipment)
    {
        if (!player.Ship.CargoHold.TryRemove(
                equipment.InstanceId,
                out _))
        {
            throw new InvalidOperationException(
                "Failed to remove equipment during purchase rollback.");
        }

        RestoreToShop(shopStock, equipment);
    }

    private static void RollbackSale(
        PlayerState player,
        ShopStock shopStock,
        EquipmentInstance equipment)
    {
        if (!shopStock.Items.TryRemove(
                equipment.InstanceId,
                out _))
        {
            throw new InvalidOperationException(
                "Failed to remove equipment during sale rollback.");
        }

        RestoreToCargoHold(player, equipment);
    }

    private static ShopTransactionResult Result(
        ShopTransactionStatus status,
        long creditsTransferred = 0)
    {
        return new ShopTransactionResult(
            status,
            creditsTransferred);
    }
}