using Habbo.Domain.Subscriptions;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages;

/// <summary>Turns a subscription into the event the client reads. Both the login push and the
/// client's own request send the same thing and differ only in why, so the reason is the one
/// thing a caller has to decide.</summary>
internal static class SubscriptionEvents
{
    public static ScrSendUserInfoComposer From(SubscriptionView subscription, SubscriptionResponseType reason) =>
        ScrSendUserInfoComposer.From(subscription, (int)reason);

    public static BuildersClubSubscriptionStatusMessageComposer From(BuildersClubView buildersClub) =>
        new(buildersClub.SecondsLeft,
            buildersClub.FurniLimit,
            buildersClub.MaxFurniLimit,
            buildersClub.SecondsLeftWithGrace);
}
