using Habbo.Protocol.Messages.Incoming.Navigator;
using Habbo.Silo.Abstractions.Navigator;

namespace Habbo.Gateway.Messages.Handlers.Navigator;

/// <summary>UpdateRoomThumbnailMessageEvent — a room's thumbnail.</summary>
internal sealed class UpdateRoomThumbnailHandler(IRoomDirectoryService service) : IMessageHandler<UpdateRoomThumbnailMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UpdateRoomThumbnailMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await service.UpdateThumbnailAsync(player, message.FlatId, message.BackgroundImageId, message.FrontImageId, message.ForegroundImageId);
        return true;
    }
}
