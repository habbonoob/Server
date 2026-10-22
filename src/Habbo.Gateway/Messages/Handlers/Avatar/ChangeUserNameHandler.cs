using Habbo.Protocol.Messages.Incoming.Avatar;

namespace Habbo.Gateway.Messages.Handlers.Avatar;

/// <summary>ChangeUserNameMessageEvent / ChangeUserNameInRoomMessageEvent — takes a new name; the
/// room sees it under the new one.</summary>
internal sealed class ChangeUserNameHandler :
    IMessageHandler<ChangeUserNameMessageEvent>,
    IMessageHandler<ChangeUserNameInRoomMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, ChangeUserNameMessageEvent message, CancellationToken ct) =>
        ChangeAsync(context, message.Name);

    public Task<bool> HandleAsync(GatewaySessionContext context, ChangeUserNameInRoomMessageEvent message, CancellationToken ct) =>
        ChangeAsync(context, message.Name);

    private static async Task<bool> ChangeAsync(GatewaySessionContext context, string name)
    {
        if (context.Session.PlayerGrain is not { } playerGrain)
            return false;

        if (await playerGrain.ChangeNameAsync(name) is { } player)
        {
            context.Session.RefreshPlayer(player);
            if (context.Session.Room is { } room)
                await room.RenamePlayerAsync(player.Id, player);
        }

        return true;
    }
}
