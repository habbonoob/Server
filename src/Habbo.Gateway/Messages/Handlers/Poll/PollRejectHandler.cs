using Habbo.Protocol.Messages.Incoming.Poll;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Poll;

/// <summary>PollRejectEvent — turn a poll down.</summary>
internal sealed class PollRejectHandler(IPollService service) : IMessageHandler<PollRejectEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PollRejectEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.RejectAsync(player, message.PollId);
        return true;
    }
}
