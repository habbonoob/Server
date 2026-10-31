namespace Habbo.Domain.Users;

/// <summary>Why money moved, as <see cref="CurrencyTransaction.Reason"/> records it.</summary>
public static class CurrencyTransactionReasons
{
    public const string CatalogPurchase = "catalog_purchase";
    public const string RespectReplenish = "respect_replenish";
    public const string Earnings = "earnings";
    public const string CreditFurniRedeem = "credit_furni_redeem";

    /// <summary>The part of a catalog purchase that bought credit furni (a gold bar, a coin): spent only until it is exchanged
    /// back, so not spending that Habbo Club Payday pays back.</summary>
    public const string CreditFurniPurchase = "credit_furni_purchase";
    public const string Grant = "grant";
    public const string GroupCreation = "group_creation";
    public const string ChestUpgrade = "chest_upgrade";
    public const string ChestWithdrawal = "chest_withdrawal";
    public const string PhotoPurchase = "photo_purchase";
    public const string PhotoPublish = "photo_publish";
    public const string RewardTrackPrize = "reward_track_prize";
    public const string DailyTaskReward = "daily_task_reward";
    public const string Recycler = "recycler";
}
