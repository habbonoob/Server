using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade;

/// <summary>WiredTradeTransactionNotificationMessageComposer — something put into a wired trade was refused</summary>
[Message(ServerMessages.WiredTradeTransactionNotificationMessageComposer)]
public sealed partial record WiredTradeTransactionNotificationMessageComposer(int NotificationId)
{
    /// <summary>What was refused, as the client has texts for ("wired_transactions.notification.trade_error.N").</summary>
    public const int InvalidItem = 0;
    public const int TooManyItems = 1;
    public const int TooManyItemTypes = 2;
    public const int TooManyCredits = 3;
    public const int RequirementsAlreadyMet = 4;
    public const int ExceedsOffer = 5;
    public const int OutOfFunds = 6;
    public const int TooManyWiredItems = 7;
    public const int TooManyWiredCredits = 8;
    public const int ExceedsChestCapacity = 9;
    public const int ChestNotFound = 10;
}
