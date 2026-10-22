using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>FriendListUpdateMessageEvent — the friend list, refreshed.</summary>
internal sealed class FriendListUpdateHandler(IMessengerService service) : IMessageHandler<FriendListUpdateMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, FriendListUpdateMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RefreshFriendListAsync(player.Id);
        return true;
    }
}
