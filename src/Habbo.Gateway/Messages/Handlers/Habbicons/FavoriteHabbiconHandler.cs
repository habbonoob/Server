using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>FavoriteHabbiconMessageEvent — star a Habbicon.</summary>
internal sealed class FavoriteHabbiconHandler(IHabbiconService habbicons) : IMessageHandler<FavoriteHabbiconMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, FavoriteHabbiconMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await habbicons.SetFavouriteAsync(player, message.HabbiconId, favourite: true);
        return true;
    }
}
