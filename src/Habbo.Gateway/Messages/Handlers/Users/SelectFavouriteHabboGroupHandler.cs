using Habbo.Protocol.Messages.Incoming.Users;
using Habbo.Silo.Abstractions.Groups;

namespace Habbo.Gateway.Messages.Handlers.Users;

/// <summary>SelectFavouriteHabboGroupMessageEvent / DeselectFavouriteHabboGroupMessageEvent — the
/// group whose badge the player shows beside their name; the room they are in sees it change.</summary>
internal sealed class SelectFavouriteHabboGroupHandler(IGroupService groups) :
    IMessageHandler<SelectFavouriteHabboGroupMessageEvent>,
    IMessageHandler<DeselectFavouriteHabboGroupMessageEvent>
{
    public Task<bool> HandleAsync(GatewaySessionContext context, SelectFavouriteHabboGroupMessageEvent message, CancellationToken ct) =>
        SetAsync(context, message.GroupId);

    public Task<bool> HandleAsync(GatewaySessionContext context, DeselectFavouriteHabboGroupMessageEvent message, CancellationToken ct) =>
        SetAsync(context, null);

    private async Task<bool> SetAsync(GatewaySessionContext context, int? groupId)
    {
        if (context.Session.Player is not { } player)
            return false;

        await groups.SetFavouriteAsync(player.Id, groupId);
        if (context.Session.Room is { } room)
            await room.FavouriteGroupChangedAsync(player.Id);
        return true;
    }
}
