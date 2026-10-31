using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>UnfavoriteHabbiconMessageEvent — unstar a Habbicon.</summary>
internal sealed class UnfavoriteHabbiconHandler(IHabbiconService habbicons) : IMessageHandler<UnfavoriteHabbiconMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UnfavoriteHabbiconMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await habbicons.SetFavouriteAsync(player, message.HabbiconId, favourite: false);
        return true;
    }
}
