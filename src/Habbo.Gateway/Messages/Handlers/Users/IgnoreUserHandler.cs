using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>IgnoreUserMessageEvent — Mutes a user's chat.</summary>
internal sealed class IgnoreUserHandler : IMessageHandler<IgnoreUserMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, IgnoreUserMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SetIgnoredAsync(message.UserId, ignored: true);
        return true;
    }
}
