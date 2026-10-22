using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Rooms;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>ApproveNameMessageEvent — whether a name would be accepted.</summary>
internal sealed class ApproveNameHandler(IPetService service) : IMessageHandler<ApproveNameMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ApproveNameMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.ApproveNameAsync(player, message.Name, message.Type);
        return true;
    }
}
