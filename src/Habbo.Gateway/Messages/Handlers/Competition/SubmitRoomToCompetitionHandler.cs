using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>SubmitRoomToCompetitionMessageEvent — a step towards entering the room the player is in.</summary>
internal sealed class SubmitRoomToCompetitionHandler(IRoomCompetitionService service) : IMessageHandler<SubmitRoomToCompetitionMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SubmitRoomToCompetitionMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        var roomId = (int)room.GetPrimaryKeyLong();
        await service.SubmitAsync(player, roomId, message.GoalCode, message.Step);
        return true;
    }
}
