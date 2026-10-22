using Habbo.Protocol.Messages.Incoming.Advertisement;

namespace Habbo.Gateway.Messages.Handlers.Advertisement;

/// <summary>GetInterstitialMessageEvent — may an ad be shown now.</summary>
internal sealed class GetInterstitialHandler : IMessageHandler<GetInterstitialMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetInterstitialMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendInterstitialAsync();
        return true;
    }
}
