using Habbo.Protocol.Messages.Incoming.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Avatar;

/// <summary>CheckUserNameMessageEvent — Whether a name would do.</summary>
internal sealed class CheckUserNameHandler : IMessageHandler<CheckUserNameMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, CheckUserNameMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.CheckNameAsync(message.Name);
        return true;
    }
}
