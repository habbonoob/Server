using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>GetOfficialRoomsMessageEvent — the official rooms view.</summary>
internal sealed class GetOfficialRoomsHandler(IRoomDirectoryService service) : IMessageHandler<GetOfficialRoomsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetOfficialRoomsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendOfficialRoomsAsync(player, message.AdIndex);
        return true;
    }
}
