using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>VoteForRoomMessageEvent — a vote for the room the player is in.</summary>
internal sealed class VoteForRoomHandler(IRoomCompetitionService service) : IMessageHandler<VoteForRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, VoteForRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        var roomId = (int)room.GetPrimaryKeyLong();
        await service.VoteAsync(player, roomId, message.GoalCode);
        return true;
    }
}
