using Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Trade.Requirements;
using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.UserDefinedRoomEvents.WiredTrading.Transactions;

/// <summary>WiredTransactionSuccessMessageComposer — a wired transaction went through; a reward carries what was given.</summary>
[Message(ServerMessages.WiredTransactionSuccessMessageComposer)]
public sealed partial record WiredTransactionSuccessMessageComposer(
    int TransactionSuccessTypeId,
    [property: WireWhen("HasReward")] TradeRequirementRuleData? RewardContents,
    [property: WireWhen("HasReward")] string? RewardText,
    [property: WireWhen("HasReward")] bool? OpenByDefault)
{
    /// <summary>The kinds of success the client has texts for ("wired_transactions.notification.success.N").</summary>
    public const int Deposited = 0;
    public const int Withdrawn = 1;
    public const int Rewarded = 2;
    public const int Paid = 3;
    public const int Traded = 4;

    public bool HasReward => RewardContents is not null;

    public static WiredTransactionSuccessMessageComposer Of(int successType) => new(successType, null, null, null);
}
