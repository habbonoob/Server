using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents.WiredTrading.Chests;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents.WiredTrading.Chests;

/// <summary>SetChestPreferencesMessageEvent — a chest's settings.</summary>
internal sealed class SetChestPreferencesHandler : IMessageHandler<SetChestPreferencesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetChestPreferencesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetChestPreferencesAsync(player.Id, message.ChestId, message.Name, message.Description, message.EveryoneCanOpen, message.EveryoneCanDonate, message.StateControlMode, message.PreviewMode, message.PreviewAmount, message.WiredEnabled);
        return true;
    }
}
