using Habbo.Protocol.Messages.Incoming.HotLooks;

namespace Habbo.Gateway.Messages.Handlers.HotLooks;

/// <summary>GetHotLooksMessageEvent — Some looks other players wear.</summary>
internal sealed class GetHotLooksHandler : IMessageHandler<GetHotLooksMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetHotLooksMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendHotLooksAsync(message.Amount);
        return true;
    }
}
