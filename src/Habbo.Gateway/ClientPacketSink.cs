using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Outgoing.Room.Session;
using Habbo.Silo.Abstractions.Hotel;
using Habbo.Silo.Abstractions.Players;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Habbo.Gateway;

/// <summary>Gateway-side implementation of the packet sink: grains push packets into
/// a session's outbound channel by session guid. Never blocks.</summary>
public sealed class ClientPacketSink(SessionManager sessions, ILogger<ClientPacketSink> logger) : IClientPacketSink
{
    public bool TrySend(Guid sessionGuid, ComposedMessage message)
    {
        var session = sessions.FindByGuid(sessionGuid);
        if (session is not { Encrypted: true })
            return false;

        return session.Enqueue(new OutboundFrame(message, Encrypted: true));
    }

    public void RefreshPlayer(Guid sessionGuid, PlayerData player) => sessions.FindByGuid(sessionGuid)?.RefreshPlayer(player);

    public void Disconnect(Guid sessionGuid) => sessions.FindByGuid(sessionGuid)?.Close();

    /// <summary>Every session in a room held there is out of it: the client goes to the hotel view as from a closed
    /// room, and the player grain stops sending its effects there.</summary>
    public void RoomHostLost(SiloAddress silo)
    {
        foreach (var session in sessions.All)
        {
            if (!session.LeaveRoomOn(silo))
                continue;
            TrySend(session.SessionGuid, new CloseConnectionMessageComposer().ToMessage());
            if (session.PlayerGrain is { } playerGrain)
                _ = ForgetRoomAsync(session, playerGrain);
        }
    }

    /// <summary>Tells the player grain it is in no room. The session may have entered another meanwhile, and the
    /// grain heard of that first: then it is told that room again, so the late clear doesn't stand.</summary>
    private async Task ForgetRoomAsync(Session session, IPlayerGrain playerGrain)
    {
        try
        {
            await playerGrain.SetCurrentRoomAsync(null);
            if (session.Room is { } room)
                await playerGrain.SetCurrentRoomAsync((int)room.GetPrimaryKeyLong());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Connection {ConnectionId}: failed to tell the player grain its room is gone", session.Id);
        }
    }
}
