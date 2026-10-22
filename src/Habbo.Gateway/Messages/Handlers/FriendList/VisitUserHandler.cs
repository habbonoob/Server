using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>VisitUserMessageEvent — go to the room a user is in.</summary>
internal sealed class VisitUserHandler(IMessengerService service) : IMessageHandler<VisitUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, VisitUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.VisitAsync(player.Id, message.UserName);
        return true;
    }
}
