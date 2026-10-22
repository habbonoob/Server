using Habbo.Protocol.Messages.Incoming.Room.Layout;

namespace Habbo.Gateway.Messages.Handlers.Room.Layout;

/// <summary>UpdateFloorPropertiesMessageEvent — save the floor plan editor's plan.</summary>
internal sealed class UpdateFloorPropertiesHandler : IMessageHandler<UpdateFloorPropertiesMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateFloorPropertiesMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player || context.Session.Room is not { } room)
            return true;

        await room.UpdateFloorPlanAsync(player, message.Heightmap, message.DoorX, message.DoorY, message.DoorDirection, message.WallThickness, message.FloorThickness, message.FixedWallsHeight);
        return true;
    }
}
