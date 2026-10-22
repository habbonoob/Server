using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>FindNewFriendsMessageEvent — go somewhere busy to meet people.</summary>
internal sealed class FindNewFriendsHandler(IMessengerService service) : IMessageHandler<FindNewFriendsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, FindNewFriendsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.FindNewFriendsAsync(player.Id);
        return true;
    }
}
