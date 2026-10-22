using Habbo.Protocol.Messages.Incoming.Advertisement;

namespace Habbo.Gateway.Messages.Handlers.Advertisement;

/// <summary>InterstitialShownMessageEvent — an ad was shown to the end.</summary>
internal sealed class InterstitialShownHandler : IMessageHandler<InterstitialShownMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, InterstitialShownMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.InterstitialShownAsync();
        return true;
    }
}
