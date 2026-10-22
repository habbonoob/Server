using Habbo.Protocol.Wire;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>2525 ScrSendUserInfoComposer — where a player's subscription stands. The client keys
/// off the product name: "habbo_club" and "club_habbo" drive the purse and the catalog, and
/// anything else it merely records.</summary>
[Message(ServerMessages.ScrSendUserInfoComposer)]
public sealed partial record ScrSendUserInfoComposer(
    string ProductName,
    int DaysToPeriodEnd,
    int MemberPeriods,
    int PeriodsSubscribedAhead,
    int ResponseType,
    bool HasEverBeenMember,
    bool IsVip,
    int PastClubDays,
    int PastVipDays,
    int MinutesUntilExpiration,
    int MinutesSinceLastModified)
{
    /// <summary>Where the subscription stands, and why the client is being told (the response type).</summary>
    public static ScrSendUserInfoComposer From(SubscriptionView subscription, int responseType) =>
        new(subscription.ProductName,
            subscription.DaysToPeriodEnd,
            subscription.MemberPeriods,
            subscription.PeriodsSubscribedAhead,
            responseType,
            subscription.HasEverBeenMember,
            subscription.IsVip,
            subscription.PastClubDays,
            subscription.PastVipDays,
            subscription.MinutesUntilExpiration,
            subscription.MinutesSinceLastModified);
}
