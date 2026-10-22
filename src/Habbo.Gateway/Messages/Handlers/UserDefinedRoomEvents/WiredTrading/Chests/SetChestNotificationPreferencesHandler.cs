using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>SetChestNotificationPreferencesMessageEvent — what a chest's owner is told.</summary>
internal sealed class SetChestNotificationPreferencesHandler : IMessageHandler<SetChestNotificationPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetChestNotificationPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetChestNotificationsAsync(player.Id, message.ChestId, message.NotifyMode, message.ChestFull, message.Donation, message.SomeoneWithdraws, message.ChestEmpty, message.WiredTransaction);
        return true;
    }
}
