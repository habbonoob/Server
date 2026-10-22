using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>2669 RequestFriend — ask somebody to be friends. When it goes through and they are
/// signed in, they hear about it at once rather than on their next look.</summary>
internal sealed class RequestFriendHandler(IFriendListService friendList, PlayerNotifier notifier)
    : IMessageHandler<RequestFriendMessageEvent>
{
    /// <summary>A friend request error answers no console message of the client's own.</summary>
    private const int NoClientMessage = 0;

    public async Task<bool> HandleAsync(
        GatewaySessionContext context, RequestFriendMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var result = await friendList.RequestFriendAsync(player.Id, message.Username, ct);
        if (!result.Sent || result.Request is null)
        {
            // asking again, or asking a friend, is simply nothing to do; the rest the client explains
            if (FriendRequestErrorCodes.Of(result.Failure) is var code and not FriendRequestErrorCodes.Unexplained)
                await context.SendAsync(new MessengerErrorComposer(NoClientMessage, code), ct);
            return true;
        }

        await notifier.SendAsync(
            result.TargetId,
            new NewFriendRequestComposer(new FriendRequestData(
                result.Request.RequesterAccountId, result.Request.RequesterName, result.Request.Figure)));

        return true;
    }
}
