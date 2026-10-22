using Habbo.Protocol.Messages.Incoming.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Avatar;

/// <summary>GetWardrobeMessageEvent — The wardrobe's saved looks.</summary>
internal sealed class GetWardrobeHandler : IMessageHandler<GetWardrobeMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetWardrobeMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendWardrobeAsync();
        return true;
    }
}
