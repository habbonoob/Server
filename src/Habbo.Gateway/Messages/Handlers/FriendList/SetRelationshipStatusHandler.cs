using Habbo.Protocol.Messages.Incoming.FriendList;
using Habbo.Silo.Abstractions.Friends;

namespace Habbo.Gateway.Messages.Handlers.FriendList;

/// <summary>SetRelationshipStatusMessageEvent — mark a friend. A player who lets only relationships see them online
/// shows online, or offline, to the friend from then on.</summary>
internal sealed class SetRelationshipStatusHandler(IMessengerService service, FriendPresenceNotifier friendPresence)
    : IMessageHandler<SetRelationshipStatusMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetRelationshipStatusMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.SetRelationshipAsync(player.Id, message.FriendId, message.Status);
        await friendPresence.AnnounceAsync(player.Id, online: true, ct);
        return true;
    }
}
