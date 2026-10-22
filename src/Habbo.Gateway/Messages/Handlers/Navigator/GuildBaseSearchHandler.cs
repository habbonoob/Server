using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>GuildBaseSearchMessageEvent — group home rooms.</summary>
internal sealed class GuildBaseSearchHandler(IRoomDirectoryService service) : IMessageHandler<GuildBaseSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GuildBaseSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.GuildBases, string.Empty, message.AdIndex);
        return true;
    }
}
