using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>GetSecondsUntilMessageEvent — the seconds until a time of the hotel's.</summary>
internal sealed class GetSecondsUntilHandler(IRoomCompetitionService service) : IMessageHandler<GetSecondsUntilMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetSecondsUntilMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendSecondsUntilAsync(player, message.TimeStr);
        return true;
    }
}
