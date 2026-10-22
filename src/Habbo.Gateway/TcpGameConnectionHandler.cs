using System.Net;
using Habbo.Domain.Rooms;
using Habbo.Gateway.Messages;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.Frames;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Habbo.Silo.Abstractions.Games;

namespace Habbo.Gateway;

/// <summary>Accepts raw game connections and drives them through the handshake into the authenticated session loop.
/// The gateway never touches the database — all state lives in grains.</summary>
public sealed class TcpGameConnectionHandler(
    SessionManager sessions,
    MessageDispatcher dispatcher,
    Messages.FriendPresenceNotifier friendPresence,
    Orleans.IGrainFactory grains,
    IOptions<CryptoOptions> cryptoOptions,
    ILogger<TcpGameConnectionHandler> logger) : ConnectionHandler
{
    private readonly RsaPrivateKey _rsaKey = cryptoOptions.Value.Rsa.ToPrivateKey();

    private readonly int _primeBits = cryptoOptions.Value.DiffieHellmanBits is var bits && bits >= 16 && bits <= DiffieHellman.MaxPrimeBits
        ? bits
        : throw new InvalidOperationException($"Crypto:DiffieHellmanBits must be 16 to {DiffieHellman.MaxPrimeBits} (the client takes 117 digits at most), not {cryptoOptions.Value.DiffieHellmanBits}");

    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        var session = sessions.Create(connection.ConnectionId, connection.RemoteEndPoint ?? new IPEndPoint(IPAddress.None, 0));
        logger.LogInformation("Connection {ConnectionId} from {Remote}", session.Id, session.RemoteEndPoint);

        var crypto = new ConnectionCrypto(_rsaKey, _primeBits);
        var context = new GatewaySessionContext
        {
            Session = session,
            Crypto = crypto,
            Output = connection.Transport.Output,
        };

        var input = connection.Transport.Input;
        var writerTask = new SessionWriter(session, crypto, connection.Transport.Output, logger)
            .RunAsync(connection.ConnectionClosed);

        // once the session stops sending — closed by the server, say for a ban — it stops reading too
        _ = writerTask.ContinueWith(_ => input.CancelPendingRead(), TaskScheduler.Default);

        try
        {
            while (!connection.ConnectionClosed.IsCancellationRequested)
            {
                var result = await input.ReadAsync(connection.ConnectionClosed);
                if (result.IsCanceled)
                    break;

                var buffer = result.Buffer;
                try
                {
                    // a session the server closed (a ban, a kick) acts on nothing more of what the client sent
                    while (!session.Closing && crypto.Decoder.TryDecode(ref buffer, out var frame))
                    {
                        var keepOpen = await dispatcher.DispatchAsync(context, frame, connection.ConnectionClosed);
                        frame.Dispose();
                        if (!keepOpen)
                        {
                            input.AdvanceTo(buffer.Start, buffer.End);
                            return;
                        }
                    }
                }
                catch (InvalidFrameException ex)
                {
                    logger.LogWarning("Connection {ConnectionId}: {Message}", session.Id, ex.Message);
                    break;
                }

                // the other end is done: a partial frame left over can never be finished, so it is a disconnect too
                // (and so is a session the server closed)
                if (result.IsCompleted || session.Closing)
                    break;

                input.AdvanceTo(buffer.Start, buffer.End);
            }
        }
        catch (Exception ex) when (ex is ConnectionResetException
            || (ex is OperationCanceledException && connection.ConnectionClosed.IsCancellationRequested))
        {
            // the client went away: a normal close, not a failure
        }
        finally
        {
            if (session.Room is { } room && session.Player is { } player)
            {
                try
                {
                    await room.LeaveSessionAsync(player.Id, session.SessionGuid);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Connection {ConnectionId}: failed to leave room", session.Id);
                }
            }

            // the rest is for a player who is now gone; one who logged in again elsewhere is still here
            var wentOffline = false;
            if (session.PlayerGrain is { } playerGrain && session.Player is not null)
            {
                try
                {
                    wentOffline = await playerGrain.SetOfflineAsync(session.SessionGuid);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Connection {ConnectionId}: failed to mark player offline", session.Id);
                }
            }

            // a player who goes stops waiting for a game, or leaves the one they play
            if (wentOffline && session.Player is { } gamer)
            {
                try
                {
                    var directory = grains.GetGrain<ISnowWarDirectoryGrain>(ISnowWarDirectoryGrain.Key);
                    await directory.LeaveAsync(gamer.Id);
                    if (await directory.GameOfAsync(gamer.Id) is { } gameId)
                        await grains.GetGrain<ISnowWarGameGrain>(gameId).ExitAsync(gamer.Id);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Connection {ConnectionId}: failed to leave SnowStorm", session.Id);
                }
            }

            if (wentOffline && session.Player is { } leaving)
            {
                try
                {
                    await friendPresence.AnnounceAsync(leaving.Id, online: false);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Connection {ConnectionId}: failed to tell friends the player left", session.Id);
                }
            }

            session.Close();
            await writerTask;
            sessions.Remove(session.Id);
            logger.LogInformation("Connection {ConnectionId} closed", session.Id);
        }
    }
}
