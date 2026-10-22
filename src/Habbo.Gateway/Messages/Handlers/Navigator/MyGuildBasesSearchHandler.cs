using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>MyGuildBasesSearchMessageEvent — the home rooms of the player's groups.</summary>
internal sealed class MyGuildBasesSearchHandler(IRoomDirectoryService service) : IMessageHandler<MyGuildBasesSearchMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MyGuildBasesSearchMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SearchAsync(player, LegacyRoomSearch.MyGuildBases, string.Empty, null);
        return true;
    }
}
