using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>2492 AcceptFriend / 69 DeclineFriend — answer pending requests. Accepting makes
/// friends of both sides, so both lists gain somebody.</summary>
internal sealed class AcceptFriendHandler(
    IFriendListService friendList,
    FriendListEvents events,
    PlayerNotifier notifier) :
    IMessageHandler<AcceptFriendMessageEvent>,
    IMessageHandler<DeclineFriendMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, AcceptFriendMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var acceptance = await friendList.AcceptFriendsAsync(player.Id, message.RequesterIds, ct);
        if (acceptance.Failures.Count > 0)
        {
            await context.SendAsync(new AcceptFriendResultComposer(acceptance.Failures
                .Select(failure => new AcceptFriendFailureData(failure.RequesterAccountId, FriendRequestErrorCodes.Of(failure.Reason)))
                .ToList()), ct);
        }

        var made = acceptance.Made;
        if (made.Count == 0)
            return true;

        await context.SendAsync(
            new FriendListUpdateComposer(
                [],
                made.Select(friendship => FriendListChangeData.Add(events.ToWire(friendship.ForPlayer)))
                    .ToList()),
            ct);

        foreach (var friendship in made)
        {
            await notifier.SendAsync(
                friendship.RequesterId,
                new FriendListUpdateComposer(
                    [], [FriendListChangeData.Add(events.ToWire(friendship.ForRequester))]));
        }

        return true;
    }

    public async Task<bool> HandleAsync(
        GatewaySessionContext context, DeclineFriendMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await friendList.DeclineFriendsAsync(player.Id, message.DeclineAll, message.RequesterIds, ct);
        return true;
    }
}
