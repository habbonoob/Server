using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Protocol.Messages.Outgoing.Catalog;
using Habbo.Silo.Abstractions.Catalog;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>GetGiftWrappingConfiguration — the wraps, boxes and ribbons a gift may come in.</summary>
internal sealed class GetGiftWrappingConfigurationHandler(IGiftWrappingService wrapping)
    : IMessageHandler<GetGiftWrappingConfigurationMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetGiftWrappingConfigurationMessageEvent message, CancellationToken ct)
    {
        var wraps = await wrapping.GetAsync(ct);
        await context.SendAsync(new GiftWrappingConfigurationComposer(
            wraps.Enabled, wraps.Price, wraps.WrapClassIds, wraps.BoxTypes, wraps.RibbonTypes, wraps.DefaultClassIds), ct);
        return true;
    }
}
