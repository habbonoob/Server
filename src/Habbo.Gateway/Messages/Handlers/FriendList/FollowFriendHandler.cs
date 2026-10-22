using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>FollowFriendMessageEvent — go to the room a friend is in.</summary>
internal sealed class FollowFriendHandler(IMessengerService service) : IMessageHandler<FollowFriendMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, FollowFriendMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.FollowAsync(player.Id, message.UserId);
        return true;
    }
}
