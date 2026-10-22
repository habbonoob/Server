using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>UnignoreUserMessageEvent — Unmutes a user.</summary>
internal sealed class UnignoreUserHandler : IMessageHandler<UnignoreUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, UnignoreUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SetIgnoredAsync(message.UserId, ignored: false);
        return true;
    }
}
