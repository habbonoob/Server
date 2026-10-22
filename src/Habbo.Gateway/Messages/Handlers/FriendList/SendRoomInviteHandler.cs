using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>SendRoomInviteMessageEvent — invite friends to the player's room.</summary>
internal sealed class SendRoomInviteHandler(IMessengerService service) : IMessageHandler<SendRoomInviteMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SendRoomInviteMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.InviteAsync(player.Id, message.UserIds, message.Message);
        return true;
    }
}
