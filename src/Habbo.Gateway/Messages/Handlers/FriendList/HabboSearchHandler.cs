using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>HabboSearchMessageEvent — find users by name.</summary>
internal sealed class HabboSearchHandler(IMessengerService service) : IMessageHandler<HabboSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, HabboSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player.Id, message.Query);
        return true;
    }
}
