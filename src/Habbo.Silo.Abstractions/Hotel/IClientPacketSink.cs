using Habbo.Silo.Abstractions.Players;
using Orleans.Runtime;

namespace Habbo.Silo.Abstractions.Hotel;

/// <summary>The one path grains may push packets toward a client session. Implemented by the
/// gateway seam; the silo never touches sockets, only this. Packets are already composed.</summary>
public interface IClientPacketSink
{
    /// <summary>Enqueues a message for the session, if it is still connected; it goes out as the session's client
    /// release reads it.</summary>
    bool TrySend(Guid sessionGuid, ComposedMessage message);

    /// <summary>Replaces the account data the session keeps from login, after the account changed.</summary>
    void RefreshPlayer(Guid sessionGuid, PlayerData player);

    /// <summary>Closes the session once what was sent to it has gone out.</summary>
    void Disconnect(Guid sessionGuid);

    /// <summary>The silo that held these rooms is gone: every session in one of them leaves it for the hotel view.</summary>
    void RoomHostLost(SiloAddress silo);
}
