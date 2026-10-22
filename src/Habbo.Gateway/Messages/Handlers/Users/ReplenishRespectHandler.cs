using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>ReplenishRespectMessageEvent — buys the day's respects back for duckets.</summary>
internal sealed class ReplenishRespectHandler : IMessageHandler<ReplenishRespectMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ReplenishRespectMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        if (await playerGrain.ReplenishRespectAsync() is { } player)
            context.Session.RefreshPlayer(player);
        return true;
    }
}
