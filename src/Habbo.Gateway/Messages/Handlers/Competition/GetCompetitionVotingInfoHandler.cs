using Habbo.Protocol.Messages.Incoming.Competition;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Competition;

/// <summary>GetCompetitionVotingInfoMessageEvent — the competition as it concerns the room gone into.</summary>
internal sealed class GetCompetitionVotingInfoHandler(IRoomCompetitionService service) : IMessageHandler<GetCompetitionVotingInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetCompetitionVotingInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        var roomId = (int)room.GetPrimaryKeyLong();
        await service.SendRoomInfoAsync(player, roomId);
        return true;
    }
}
