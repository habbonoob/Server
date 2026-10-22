using Habbo.Protocol.Messages.Incoming.Notifications;

namespace Habbo.Gateway.Messages.Handlers.Notifications;

/// <summary>2303 ResetUnseenItems / 1499 ResetUnseenItemIds — the player has now looked at a
/// category, or at particular items, so the client is clearing the badge on its own.
///
/// Accepted and dropped. The unseen set is only ever built from what we push after a purchase
/// and lives in the client for the session; there is nothing stored to clear. Handling them
/// still matters: an unhandled message would be logged as unexpected on every inventory open.</summary>
internal sealed class ResetUnseenItemsHandler :
    IMessageHandler<ResetUnseenItemsEvent>,
    IMessageHandler<ResetUnseenItemIdsEvent>
{
    public Task<bool> HandleAsync(
        GatewaySessionContext context, ResetUnseenItemsEvent message, CancellationToken ct)
        => Task.FromResult(true);

    public Task<bool> HandleAsync(
        GatewaySessionContext context, ResetUnseenItemIdsEvent message, CancellationToken ct)
        => Task.FromResult(true);
}
