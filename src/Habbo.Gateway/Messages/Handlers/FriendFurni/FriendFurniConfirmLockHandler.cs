using Habbo.Protocol.Messages.Incoming.FriendFurni;

namespace Habbo.Gateway.Messages.Handlers.FriendFurni;

/// <summary>FriendFurniConfirmLockMessageEvent — confirm or turn down locking a friendship furni.</summary>
internal sealed class FriendFurniConfirmLockHandler : IMessageHandler<FriendFurniConfirmLockMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, FriendFurniConfirmLockMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.ConfirmFriendLockAsync(player.Id, message.StuffId, message.Confirmed);
        return true;
    }
}
