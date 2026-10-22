using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>GetIsUserPartOfCompetitionMessageEvent — whether the player has entered a room.</summary>
internal sealed class GetIsUserPartOfCompetitionHandler(IRoomCompetitionService service) : IMessageHandler<GetIsUserPartOfCompetitionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetIsUserPartOfCompetitionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendIsPartOfAsync(player, message.GoalCode);
        return true;
    }
}
