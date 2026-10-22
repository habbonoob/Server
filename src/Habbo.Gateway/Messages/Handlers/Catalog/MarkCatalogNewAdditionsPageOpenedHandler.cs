using Habbo.Protocol.Messages.Incoming.Catalog;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Catalog;

/// <summary>MarkCatalogNewAdditionsPageOpened — remembers when the player last looked at what is new.</summary>
internal sealed class MarkCatalogNewAdditionsPageOpenedHandler : IMessageHandler<MarkCatalogNewAdditionsPageOpenedEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MarkCatalogNewAdditionsPageOpenedEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.UpdatePreferencesAsync(new PreferencesUpdate(CatalogNewAdditionsSeenAt: DateTimeOffset.UtcNow));
        return true;
    }
}
