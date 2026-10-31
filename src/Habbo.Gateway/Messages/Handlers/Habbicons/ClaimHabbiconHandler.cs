using Habbo.Protocol.Messages.Incoming.Habbicons;
using Habbo.Silo.Abstractions.Habbicons;

namespace Habbo.Gateway.Messages.Handlers.Habbicons;

/// <summary>ClaimHabbiconMessageEvent — claim a complete set's reward Habbicon.</summary>
internal sealed class ClaimHabbiconHandler(IHabbiconService habbicons) : IMessageHandler<ClaimHabbiconMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ClaimHabbiconMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await habbicons.ClaimAsync(player, message.HabbiconId);
        return true;
    }
}
