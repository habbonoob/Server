using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>BlockUserMessageEvent — Blocks a user.</summary>
internal sealed class BlockUserHandler : IMessageHandler<BlockUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, BlockUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SetBlockedAsync(message.UserId, blocked: true);
        return true;
    }
}
