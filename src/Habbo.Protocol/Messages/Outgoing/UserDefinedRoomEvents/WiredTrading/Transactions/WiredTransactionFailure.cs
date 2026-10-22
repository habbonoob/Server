namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>Why a wired transaction or trade failed, as the client has texts for ("wired_transactions.notification.fail.N").</summary>
public static class WiredTransactionFailure
{
    public const int UserCancelled = 0;
    public const int InvalidTrade = 1;
    public const int Timeout = 2;
    public const int TradeCancelled = 3;
    public const int AlreadyTrading = 4;
    public const int WiredMisconfiguration = 5;
    public const int InsufficientFunds = 6;
    public const int FundsNoLongerAvailable = 7;
    public const int UserCannotTrade = 8;
    public const int ChestOwnerCannotTrade = 9;
    public const int EmptyTransaction = 10;
    public const int ChestFull = 11;
    public const int FeatureDisabled = 12;
    public const int ChestNotInRoom = 13;
    public const int TooManyChests = 14;
    public const int NoOrLockedChests = 15;
    public const int AllContentsToManyUsers = 16;
    public const int TooManyOffers = 17;
    public const int TradingTooFast = 18;
    public const int ExceedsChestCapacity = 19;
    public const int InternalError = 1000;
}
