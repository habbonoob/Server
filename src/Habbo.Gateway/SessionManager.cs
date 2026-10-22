using System.Collections.Concurrent;
using System.Net;

namespace Habbo.Gateway;

/// <summary>Tracks every open connection so shutdown can disconnect clients cleanly,
/// and so grains can reach sessions through the packet sink.</summary>
public sealed class SessionManager
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    /// <summary>The same sessions by their guid, which every grain-to-client send looks them up by.</summary>
    private readonly ConcurrentDictionary<Guid, Session> _byGuid = new();

    /// <summary>The logged-in sessions by player, so a notice for a player (one per friend, at every login and logout)
    /// finds theirs without going through every session in the hotel.</summary>
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, Session>> _byPlayer = new();

    public Session Create(string id, EndPoint remoteEndPoint)
    {
        var session = _sessions.GetOrAdd(id, _ => new Session { Id = id, RemoteEndPoint = remoteEndPoint, PlayerBound = Index });
        _byGuid[session.SessionGuid] = session;
        return session;
    }

    public void Remove(string id)
    {
        if (!_sessions.TryRemove(id, out var session))
            return;

        _byGuid.TryRemove(session.SessionGuid, out _);
        if (session.Player is { } player && _byPlayer.TryGetValue(player.Id, out var theirs))
        {
            theirs.TryRemove(id, out _);
            if (theirs.IsEmpty)
                _byPlayer.TryRemove(new KeyValuePair<Guid, ConcurrentDictionary<string, Session>>(player.Id, theirs));
        }
    }

    public Session? FindByGuid(Guid sessionGuid) => _byGuid.GetValueOrDefault(sessionGuid);

    public IReadOnlyCollection<Session> All => _sessions.Values.ToArray();

    /// <summary>Every session a player has open. A player can be signed in more than once, and
    /// anything pushed at them — a message, a friend coming online — belongs on all of them.
    /// </summary>
    public IEnumerable<Session> FindByPlayer(Guid playerId) =>
        _byPlayer.TryGetValue(playerId, out var theirs) ? theirs.Values : [];

    private void Index(Session session)
    {
        // a session removed while it logged in is not put back
        if (session.Player is { } player && _sessions.ContainsKey(session.Id))
            _byPlayer.GetOrAdd(player.Id, _ => new ConcurrentDictionary<string, Session>())[session.Id] = session;
    }
}
