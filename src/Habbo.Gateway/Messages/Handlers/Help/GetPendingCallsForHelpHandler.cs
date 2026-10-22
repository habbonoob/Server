using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>GetPendingCallsForHelpMessageEvent — the player's open calls.</summary>
internal sealed class GetPendingCallsForHelpHandler(IModerationService service) : IMessageHandler<GetPendingCallsForHelpMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetPendingCallsForHelpMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendPendingCallsAsync(player.Id);
        return true;
    }
}
