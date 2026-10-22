using Habbo.Protocol.Messages.Incoming.Users;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>ScrGetKickbackInfoMessageEvent — The club payday figures.</summary>
internal sealed class ScrGetKickbackInfoHandler : IMessageHandler<ScrGetKickbackInfoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ScrGetKickbackInfoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await playerGrain.SendKickbackInfoAsync();
        return true;
    }
}
