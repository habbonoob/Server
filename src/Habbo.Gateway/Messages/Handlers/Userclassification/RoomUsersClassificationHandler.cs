using Habbo.Protocol.Messages.Incoming.UserClassification;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.UserClassification;

/// <summary>RoomUsersClassificationMessageEvent — the users of a class in the room.</summary>
internal sealed class RoomUsersClassificationHandler(IUserClassificationService service) : IMessageHandler<RoomUsersClassificationMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomUsersClassificationMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SendRoomAsync(player, context.Session.Room is { } room ? await room.GetUserIdsAsync() : [], message.Classification);
        return true;
    }
}
