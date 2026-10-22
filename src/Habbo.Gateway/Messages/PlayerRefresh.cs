using Habbo.Silo.Abstractions.Players;

namespace Habbo.Gateway.Messages;

/// <summary>Spreads a change to the player's account to what caches it: the session, and the room
/// they are in, which shows everyone the new them.</summary>
internal static class PlayerRefresh
{
    public static async Task RefreshAsync(GatewaySessionContext context, PlayerData player)
    {
        context.Session.RefreshPlayer(player);
        if (context.Session.Room is { } room)
            await room.RefreshPlayerAsync(player.Id, player);
    }
}
