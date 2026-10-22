using System.Collections.Concurrent;

namespace Habbo.Silo.Rooms;

/// <summary>The staff who went invisible (:invisible): the rooms they enter show them to nobody else, until they
/// become visible again. Held for as long as the server runs.</summary>
public sealed class InvisiblePlayers
{
    private readonly ConcurrentDictionary<Guid, bool> _invisible = new();

    public bool IsInvisible(Guid playerId) => _invisible.ContainsKey(playerId);

    /// <summary>Turns the player's invisibility over; whether they are invisible now.</summary>
    public bool Toggle(Guid playerId)
    {
        if (_invisible.TryRemove(playerId, out _))
            return false;

        _invisible[playerId] = true;
        return true;
    }
}
