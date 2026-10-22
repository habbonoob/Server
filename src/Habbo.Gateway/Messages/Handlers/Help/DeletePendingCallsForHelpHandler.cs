using Habbo.Protocol.Messages.Incoming.Help;
using Habbo.Silo.Abstractions.Moderation;

namespace Habbo.Gateway.Messages.Handlers.Help;

/// <summary>DeletePendingCallsForHelpMessageEvent — drop the player's open calls.</summary>
internal sealed class DeletePendingCallsForHelpHandler(IModerationService service) : IMessageHandler<DeletePendingCallsForHelpMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, DeletePendingCallsForHelpMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.DeletePendingCallsAsync(player.Id);
        return true;
    }
}
