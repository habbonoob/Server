using Habbo.Silo.Abstractions.Friends;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway.Messages;

/// <summary>Tells a player's friends when they sign in or out. Their friend lists have no other
/// way of finding out: the list is read once when the messenger starts, so without this a friend
/// who arrives afterwards stays greyed out until the client is restarted. Friends the player hides
/// being online from are told they are offline; announcing again after the player changes who
/// may see them puts each friend's list right. Going into and out of rooms the player grain
/// announces itself.</summary>
public sealed class FriendPresenceNotifier(
    IFriendListService friendList,
    ILogger<FriendPresenceNotifier> logger)
{
    public async Task AnnounceAsync(Guid playerId, bool online, CancellationToken ct = default)
    {
        try
        {
            await friendList.AnnouncePresenceAsync(playerId, online, ct);
        }
        catch (Exception ex)
        {
            // Signing in or out must not fail because their friends could not be told.
            logger.LogWarning(ex, "Could not tell the friends of {PlayerId} they are {State}",
                playerId, online ? "online" : "offline");
        }
    }
}
