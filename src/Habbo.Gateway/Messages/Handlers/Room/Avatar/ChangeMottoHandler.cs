using Habbo.Protocol.Messages.Incoming.Room.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Room.Avatar;

/// <summary>ChangeMottoMessageEvent.</summary>
internal sealed class ChangeMottoHandler : IMessageHandler<ChangeMottoMessageEvent>
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, ChangeMottoMessageEvent message, CancellationToken ct)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        await PlayerRefresh.RefreshAsync(context, await playerGrain.UpdateMottoAsync(message.Motto));
        return true;
    }
}
