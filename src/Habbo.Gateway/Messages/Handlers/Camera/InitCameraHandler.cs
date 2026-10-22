using Habbo.Camera;
using Habbo.Protocol.Messages.Incoming.Camera;
using Habbo.Protocol.Messages.Outgoing.Camera;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages.Handlers.Camera;

/// <summary>InitCameraMessageEvent — what a photo costs, which the camera's purchase dialog shows.</summary>
internal sealed class InitCameraHandler(IOptions<CameraOptions> options) : IMessageHandler<InitCameraMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, InitCameraMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is null)
            return false;

        var prices = options.Value;
        await context.SendAsync(new InitCameraMessageComposer(prices.CreditPrice, prices.DucketPrice, prices.PublishDucketPrice), ct);
        return true;
    }
}
