using Habbo.Domain.Subscriptions;
using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Subscriptions;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>829 ScrGetUserInfo — answer with where the named subscription stands. Asked for by
/// the client rather than pushed, so it reports rather than announces a change.</summary>
internal sealed class ScrGetUserInfoHandler(ISubscriptionService subscriptions)
    : IMessageHandler<ScrGetUserInfoMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, ScrGetUserInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var subscription = await subscriptions.GetAsync(player.Id, message.ProductName, ct);
        await context.SendAsync(SubscriptionEvents.From(subscription, SubscriptionResponseType.Info), ct);
        return true;
    }
}
