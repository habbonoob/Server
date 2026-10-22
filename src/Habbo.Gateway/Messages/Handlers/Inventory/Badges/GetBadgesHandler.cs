using Habbo.Protocol.Messages.Incoming.Inventory.Badges;

namespace Habbo.Gateway.Messages.Handlers.Inventory.Badges;

/// <summary>GetBadgesEvent — The badge inventory.</summary>
internal sealed class GetBadgesHandler : IMessageHandler<GetBadgesEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetBadgesEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendBadgesAsync();
        return true;
    }
}
