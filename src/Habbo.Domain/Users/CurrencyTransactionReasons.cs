namespace Habbo.Domain.Users;

/// <summary>Why money moved, as <see cref="CurrencyTransaction.Reason"/> records it.</summary>
public static class CurrencyTransactionReasons
{
    public const string CatalogPurchase = "catalog_purchase";
    public const string RespectReplenish = "respect_replenish";
    public const string Earnings = "earnings";
    public const string CreditFurniRedeem = "credit_furni_redeem";
    public const string Grant = "grant";
    public const string GroupCreation = "group_creation";
    public const string ChestUpgrade = "chest_upgrade";
    public const string ChestWithdrawal = "chest_withdrawal";
    public const string PhotoPurchase = "photo_purchase";
    public const string PhotoPublish = "photo_publish";
}
