using Habbo.Protocol.Messages.Incoming.Poll;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Poll;

/// <summary>PollStartEvent — take a poll.</summary>
internal sealed class PollStartHandler(IPollService service) : IMessageHandler<PollStartEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PollStartEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.StartAsync(player, message.PollId);
        return true;
    }
}
