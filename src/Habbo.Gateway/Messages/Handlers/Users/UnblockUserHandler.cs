using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>UnblockUserMessageEvent — Unblocks a user.</summary>
internal sealed class UnblockUserHandler : IMessageHandler<UnblockUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UnblockUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SetBlockedAsync(message.UserId, blocked: false);
        return true;
    }
}
