using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>ChangeEmailEvent — Changes the email address on file.</summary>
internal sealed class ChangeEmailHandler : IMessageHandler<ChangeEmailEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ChangeEmailEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.ChangeEmailAsync(message.Email);
        return true;
    }
}
