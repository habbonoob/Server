using Habbo.Protocol.Messages.Incoming.Customfilter;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Customfilter;

/// <summary>RemoveFromCustomFilterMessageEvent — stop filtering a word.</summary>
internal sealed class RemoveFromCustomFilterHandler(IPlayerSettingsService service) : IMessageHandler<RemoveFromCustomFilterMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RemoveFromCustomFilterMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RemoveFilterWordAsync(player, message.Word);
        return true;
    }
}
