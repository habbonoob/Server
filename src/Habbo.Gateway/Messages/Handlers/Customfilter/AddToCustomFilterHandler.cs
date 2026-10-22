using Habbo.Protocol.Messages.Incoming.Customfilter;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Customfilter;

/// <summary>AddToCustomFilterMessageEvent — filter a word.</summary>
internal sealed class AddToCustomFilterHandler(IPlayerSettingsService service) : IMessageHandler<AddToCustomFilterMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, AddToCustomFilterMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.AddFilterWordAsync(player, message.Word);
        return true;
    }
}
