using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Protocol.Messages.Outgoing.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>508 GetFriendRequests — what is waiting for the player to answer.</summary>
internal sealed class GetFriendRequestsHandler(IFriendListService friendList)
    : IMessageHandler<GetFriendRequestsMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetFriendRequestsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var requests = await friendList.GetFriendRequestsAsync(player.Id, ct);
        await context.SendAsync(
            new FriendRequestsComposer(
                requests.Count,
                requests
                    .Select(request => new FriendRequestData(
                        request.RequesterAccountId, request.RequesterName, request.Figure))
                    .ToList()),
            ct);
        return true;
    }
}
