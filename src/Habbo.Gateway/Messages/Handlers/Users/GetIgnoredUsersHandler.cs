using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Protocol.Messages.Outgoing.Users;
using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>2938 GetIgnoredUsers / 1613 BlockListInit — who the player has muted, and who they
/// have blocked. Two lists the client keeps apart, so they are answered apart.</summary>
internal sealed class GetIgnoredUsersHandler(IUserRelationsService relations) :
    IMessageHandler<GetIgnoredUsersMessageEvent>,
    IMessageHandler<GetBlockedUsersMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetIgnoredUsersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await context.SendAsync(
            new IgnoredUsersMessageComposer(await relations.GetIgnoredAsync(player.Id, ct)), ct);
        return true;
    }

    public async Task<bool> HandleAsync(
        GatewaySessionContext context, GetBlockedUsersMessageEvent message, CancellationToken ct)
    {
        if (context.Session.Player is not { } player)
            return false;

        await context.SendAsync(
            new BlockListMessageComposer(await relations.GetBlockedAsync(player.Id, ct)), ct);
        return true;
    }
}
