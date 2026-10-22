using Habbo.Camera;
using Habbo.Protocol.Messages.Incoming.Camera;
using Habbo.Protocol.Messages.Outgoing.Camera;
using Habbo.Silo.Abstractions.Camera;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway.Messages.Handlers.Camera;

/// <summary>RenderRoomThumbnailMessageEvent — the room's navigator thumbnail, rendered as a photo is and kept as the
/// room's (navigator.thumbnail.url_base, then the room's id and ".png"). Only the room's owner, or staff who pick
/// rooms, may.</summary>
internal sealed class RenderRoomThumbnailHandler(
    PhotoRenderer renderer,
    PhotoStore store,
    RenderLimit limit,
    ICameraService camera,
    ILogger<RenderRoomThumbnailHandler> logger) : IMessageHandler<RenderRoomThumbnailMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RenderRoomThumbnailMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        // no more renders than the limit, counted before the request is read: inflating it is costly already
        if (!limit.TryTake(player.Id))
        {
            await context.SendAsync(new ThumbnailStatusMessageComposer(false, true), ct);
            return true;
        }

        RenderRequest request;
        try
        {
            request = RenderRequestDecoder.Decode(message.Data.ToArray());
        }
        catch (Exception e) when (e is InvalidDataException or System.Text.Json.JsonException)
        {
            logger.LogWarning("Connection {ConnectionId}: an unreadable room thumbnail ({Error})", context.Session.Id, e.Message);
            await context.SendAsync(new ThumbnailStatusMessageComposer(false, false), ct);
            return true;
        }

        if (context.Session.Room?.GetPrimaryKeyLong() != request.RoomId || !await camera.MayPhotographRoomAsync(player, request.RoomId))
        {
            await context.SendAsync(new ThumbnailStatusMessageComposer(false, false), ct);
            return true;
        }
        var thumbnail = await renderer.RenderAsync(request, ct);
        await store.SaveThumbnailAsync(request.RoomId, thumbnail.Png, ct);
        await context.SendAsync(new ThumbnailStatusMessageComposer(true, false), ct);
        return true;
    }
}
