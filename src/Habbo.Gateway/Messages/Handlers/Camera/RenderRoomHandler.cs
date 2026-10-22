using Habbo.Camera;
using Habbo.Protocol.Messages.Incoming.Camera;
using Habbo.Protocol.Messages.Outgoing.Camera;
using Habbo.Silo.Abstractions.Camera;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages.Handlers.Camera;

/// <summary>RenderRoomMessageEvent — the camera's photo: rendered from the room as the client composed it, kept, and
/// its name told to the client, which shows it in the purchase dialog. An empty name tells it the photo could not be
/// had (camera.render.count.info).</summary>
internal sealed class RenderRoomHandler(
    PhotoRenderer renderer,
    PhotoStore store,
    RenderLimit limit,
    ICameraService camera,
    IOptions<CameraOptions> options,
    ILogger<RenderRoomHandler> logger) : IMessageHandler<RenderRoomMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, RenderRoomMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        // no more photos than the limit, counted before the request is read: inflating it is costly already
        if (!limit.TryTake(player.Id))
        {
            await context.SendAsync(new CameraStorageUrlMessageComposer(string.Empty), ct);
            return true;
        }

        string json;
        RenderRequest request;
        try
        {
            json = RenderRequestDecoder.Inflate(message.Data.ToArray());
            request = RenderRequestDecoder.Parse(json);
        }
        catch (Exception e) when (e is InvalidDataException or System.Text.Json.JsonException)
        {
            logger.LogWarning("Connection {ConnectionId}: an unreadable photo ({Error})", context.Session.Id, e.Message);
            await context.SendAsync(new CameraStorageUrlMessageComposer(string.Empty), ct);
            return true;
        }

        // a photo of the room the player is in
        if (context.Session.Room?.GetPrimaryKeyLong() != request.RoomId)
        {
            await context.SendAsync(new CameraStorageUrlMessageComposer(string.Empty), ct);
            return true;
        }

        var photo = await renderer.RenderAsync(request, ct);
        var id = await camera.RecordPhotoAsync(player, request.RoomId);
        await store.SavePhotoAsync(id, photo, ct);
        if (options.Value.KeepRenderRequests)
            await store.KeepRequestAsync(id, json, ct);

        await context.SendAsync(new CameraStorageUrlMessageComposer(PhotoStore.PhotoName(id)), ct);
        return true;
    }
}
