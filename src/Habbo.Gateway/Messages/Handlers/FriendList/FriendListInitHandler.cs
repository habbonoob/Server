using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>1276 MessengerInit — set the messenger up and send the friend list after it. The
/// client waits for both: the limits tell it what to draw, the fragments fill it in.</summary>
internal sealed class FriendListInitHandler(IFriendListService friendList, IMessengerService messenger, FriendListEvents events)
    : IMessageHandler<MessengerInitMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, MessengerInitMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var view = await friendList.GetFriendListAsync(player.Id, ct);
        await context.SendAsync(events.Init(view), ct);
        foreach (var fragment in events.Fragments(view))
            await context.SendAsync(fragment, ct);

        // what arrived while the player was away
        await messenger.DeliverPendingAsync(player.Id);

        return true;
    }
}
