using Habbo.Camera;
using Habbo.Protocol.Messages.Incoming.Camera;
using Habbo.Protocol.Messages.Outgoing.Camera;
using Habbo.Silo.Abstractions.Camera;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages.Handlers.Camera;

/// <summary>PublishPhotoMessageEvent — the photo just taken, published to the player's stories for the camera's
/// price; refused, with how long to wait, when they published one too recently.</summary>
internal sealed class PublishPhotoHandler(ICameraService camera, IOptions<CameraOptions> options) : IMessageHandler<PublishPhotoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, PublishPhotoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var publication = await camera.PublishAsync(player, options.Value.PublishDucketPrice,
            TimeSpan.FromSeconds(options.Value.PublishCooldownSeconds));
        await context.SendAsync(new CameraPublishStatusMessageComposer(publication.Published, publication.SecondsToWait,
            publication.PhotoId?.ToString("N")), ct);
        return true;
    }
}
