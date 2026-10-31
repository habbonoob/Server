using Habbo.Protocol.Messages.Incoming.UserDefinedRoomEvents;

namespace Habbo.Gateway.Messages.Handlers.UserDefinedRoomEvents;

/// <summary>SetFurnitureLabelSettingsMessageEvent — a wired label's colour, who sees it and whether it stays open.</summary>
internal sealed class SetFurnitureLabelSettingsHandler : IMessageHandler<SetFurnitureLabelSettingsMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, SetFurnitureLabelSettingsMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SetFurnitureLabelSettingsAsync(player.Id, message.ItemId, message.BubbleColor, message.Visibility, message.AlwaysOpen);
        return true;
    }
}
