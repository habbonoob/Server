using Habbo.Protocol.Messages.Incoming.Room.Furniture;

namespace Habbo.Gateway.Messages.Handlers.Room.Furniture;

/// <summary>RoomDimmerSavePresetMessageEvent — save a dimmer preset.</summary>
internal sealed class RoomDimmerSavePresetHandler : IMessageHandler<RoomDimmerSavePresetMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RoomDimmerSavePresetMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.SaveDimmerPresetAsync(player.Id, message.ItemId, message.PresetNumber, message.EffectType, message.ColorHex, message.Brightness, message.Apply);
        return true;
    }
}
