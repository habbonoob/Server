using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>2484 RemoveFriend — drop friends and tell both sides. The other side is told too:
/// their list would otherwise keep somebody who no longer has them.</summary>
internal sealed class RemoveFriendHandler(IFriendListService friendList, PlayerNotifier notifier)
    : IMessageHandler<RemoveFriendMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, RemoveFriendMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var removed = await friendList.RemoveFriendsAsync(player.Id, message.FriendIds, ct);
        if (removed.Count == 0)
            return true;

        await context.SendAsync(
            new FriendListUpdateComposer(
                [],
                removed.Select(friend => FriendListChangeData.Remove(friend.FriendAccountId)).ToList()),
            ct);

        foreach (var friend in removed)
        {
            await notifier.SendAsync(
                friend.FriendId,
                new FriendListUpdateComposer([], [FriendListChangeData.Remove(player.AccountId)]));
        }

        return true;
    }
}
