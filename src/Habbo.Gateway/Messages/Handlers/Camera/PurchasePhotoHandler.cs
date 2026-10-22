using Habbo.Camera;
using Habbo.Protocol.Messages.Incoming.Camera;
using Habbo.Protocol.Messages.Outgoing.Camera;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Camera;
using Microsoft.Extensions.Options;

namespace Habbo.Gateway.Messages.Handlers.Camera;

/// <summary>PurchasePhotoMessageEvent — the photo just taken, bought as a photo furni for the camera's prices. The
/// new balances and the furni are pushed by the player's grain as it spends and grants.</summary>
internal sealed class PurchasePhotoHandler(ICameraService camera, PhotoStore store, IOptions<CameraOptions> options)
    : IMessageHandler<PurchasePhotoMessageEvent>
{
    private const int DucketType = (int)Habbo.Domain.Catalog.ActivityPointType.Ducket;

    public async Task<bool> HandleAsync(GatewaySessionContext context, PurchasePhotoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        var prices = options.Value;
        var purchase = await camera.PurchaseAsync(player, prices.CreditPrice, prices.DucketPrice, store.PhotoUrl);
        if (purchase.Success)
        {
            // the session holds the player from login; the credits spent have to show there too
            if (context.Session.PlayerGrain is { } grain)
                context.Session.RefreshPlayer(await grain.GetDataAsync());
            await context.SendAsync(new CameraPurchaseOKMessageComposer(), ct);
        }
        else if (purchase.NotEnoughCredits || purchase.NotEnoughDuckets)
        {
            await context.SendAsync(new NotEnoughBalanceMessageComposer(purchase.NotEnoughCredits, purchase.NotEnoughDuckets,
                purchase.NotEnoughDuckets ? DucketType : null), ct);
        }
        return true;
    }
}
