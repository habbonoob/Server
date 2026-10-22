using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>GetEmailStatusEvent — The email address on file.</summary>
internal sealed class GetEmailStatusHandler : IMessageHandler<GetEmailStatusEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, GetEmailStatusEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendEmailStatusAsync();
        return true;
    }
}
