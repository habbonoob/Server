using System.Net;
using System.Threading.Channels;
using Habbo.Domain.Rooms;
using Habbo.Protocol.Releases;
using Habbo.Silo.Abstractions.Players;
using Habbo.Silo.Abstractions.Rooms;
using Orleans;
using Orleans.Runtime;

namespace Habbo.Gateway;

/// <summary>A single client connection. Owned by the gateway seam; the silo never sees it.
/// All outbound traffic flows through the channel and is encoded by the session's writer task,
/// so the RC4 state is only ever touched from one place.</summary>
public sealed class Session
{
    public required string Id { get; init; }
    public required EndPoint RemoteEndPoint { get; init; }
    public Guid SessionGuid { get; } = Guid.NewGuid();

    /// <summary>The most frames that may wait to be written: a client that stops reading fills it, and is closed then
    /// instead of holding everything sent to it in memory.</summary>
    public const int OutboundLimit = 4096;

    public Channel<OutboundFrame> Outbound { get; } = Channel.CreateBounded<OutboundFrame>(
        new BoundedChannelOptions(OutboundLimit) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });

    /// <summary>Queues a frame to be written. A session whose queue is full is closed: whatever it is behind on,
    /// it would never catch up. False when the frame was not queued.</summary>
    public bool Enqueue(OutboundFrame frame)
    {
        if (Outbound.Writer.TryWrite(frame))
            return true;

        Close();
        return false;
    }

    private volatile bool _closing;

    /// <summary>Whether the server has closed the session: what the client already sent is not acted on any more.</summary>
    public bool Closing => _closing;

    /// <summary>Closes the session: what is queued still goes out, then the connection closes.</summary>
    public void Close()
    {
        _closing = true;
        Outbound.Writer.TryComplete();
    }

    public PlayerData? Player { get; private set; }
    public IPlayerGrain? PlayerGrain { get; private set; }
    public IGameRoomGrain? Room { get; private set; }

    /// <summary>The silo <see cref="Room"/> is held on: when it goes, the session is out of the room.</summary>
    public SiloAddress? RoomHost { get; private set; }

    /// <summary>Whether the handshake is done and its last plaintext frame queued: what is sent from outside the
    /// connection (pings, grains) waits for this, since a frame queued as encrypted before it would be written with
    /// the cipher before the client has installed it.</summary>
    public volatile bool Encrypted;

    /// <summary>The messages the connection may still send at once, and when that was last worked out: a token
    /// bucket the dispatcher fills at the allowed rate and takes one from per message.</summary>
    public double MessageAllowance { get; set; } = double.NaN;
    public long MessageAllowanceAt { get; set; }

    /// <summary>When the client last answered a ping, or connected; one silent too long is closed.</summary>
    public DateTimeOffset LastPongAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the connection was made; one still not logged in long after is closed, pongs or not.</summary>
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>The client's release's headers, set by its ClientHello; until then (and for a release the server
    /// has no table for) the native ones.</summary>
    public ReleaseHeaderMap Headers { get; set; } = ReleaseHeaderMap.Native;

    /// <summary>The machine id the client sent before logging in; bans may cover the machine.</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Moves the session into a room, or out of every room (null). The player grain is
    /// told too, since what runs out on its own — an effect — has to reach the room it is worn in.</summary>
    public async Task SetRoomAsync(IGameRoomGrain? room)
    {
        Room = room;
        RoomHost = room is null ? null : await room.GetHostAsync();
        if (PlayerGrain is { } playerGrain)
            await playerGrain.SetCurrentRoomAsync(room is null ? null : (int)room.GetPrimaryKeyLong());
    }

    /// <summary>Leaves the room the session is in. The room is left first: the session keeps it until then, so a
    /// failure leaves it for the next try (or the disconnect) rather than an avatar in a room no session knows of.</summary>
    public async Task LeaveRoomAsync()
    {
        if (Room is not { } room || Player is not { } player)
            return;
        await room.LeaveSessionAsync(player.Id, SessionGuid);
        await SetRoomAsync(null);
    }

    /// <summary>Out of the room, if it was held on that silo, which is gone: true when it was.</summary>
    public bool LeaveRoomOn(SiloAddress silo)
    {
        if (Room is null || RoomHost is null || !RoomHost.Equals(silo))
            return false;
        Room = null;
        RoomHost = null;
        return true;
    }

    /// <summary>Marks the session authenticated; called once after the SSO handshake succeeds.</summary>
    public void BindPlayer(PlayerData player, IPlayerGrain grain)
    {
        Player = player;
        PlayerGrain = grain;
        PlayerBound?.Invoke(this);
    }

    /// <summary>Told when the session logs in, so the session manager can find it by its player.</summary>
    internal Action<Session>? PlayerBound { get; set; }

    /// <summary>Replaces the cached player view after the grain has changed it. The session
    /// caches PlayerData from login, so anything that reads it — the hotel view replaying
    /// InfoRetrieve, for one — would otherwise keep reporting the balance as it was then.</summary>
    public void RefreshPlayer(PlayerData player) => Player = player;

    /// <summary>Sets the balance a purchase left on the cached player view, and only that: the rest stays as the
    /// grain last pushed it, which may be newer than the copy the purchase started from.</summary>
    public void RefreshCredits(int credits)
    {
        if (Player is { } player)
            Player = player with { Credits = credits };
    }
}
