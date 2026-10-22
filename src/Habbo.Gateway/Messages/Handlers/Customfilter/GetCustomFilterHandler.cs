using Habbo.Protocol.Messages.Incoming.Customfilter;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Customfilter;

/// <summary>GetCustomFilterMessageEvent — the player's chat filter.</summary>
internal sealed class GetCustomFilterHandler(IPlayerSettingsService service) : IMessageHandler<GetCustomFilterMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCustomFilterMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendFilterAsync(player);
        return true;
    }
}
