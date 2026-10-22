using System.Buffers;
using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.Extensions.Options;

namespace Habbo.Relay;

/// <summary>
/// Carries a client's WebSocket to a game server's TCP socket and back, byte for byte. The browser client can only
/// open WebSockets; Habbo's game servers speak TCP to the desktop client. Nothing is read or changed on the way: the
/// traffic is encrypted after the handshake, and the client records what it sends and receives itself (/capture).
/// </summary>
public sealed class GameRelay(IOptions<RelayOptions> options, ILogger<GameRelay> logger)
{
    private const int BufferSize = 64 * 1024;

    /// <summary>Whether the relay may carry a client to this host.</summary>
    public bool IsAllowed(string host)
    {
        foreach (var pattern in options.Value.AllowedHosts)
        {
            if (pattern.StartsWith("*.", StringComparison.Ordinal)
                    ? host.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase)
                    : string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public async Task RunAsync(WebSocket socket, string host, int port, CancellationToken ct)
    {
        using var tcp = new TcpClient { NoDelay = true };
        try
        {
            await tcp.ConnectAsync(host, port, ct);
        }
        catch (SocketException e)
        {
            logger.LogWarning("Could not reach {Host}:{Port}: {Error}", host, port, e.Message);
            await socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "game server unreachable", ct);
            return;
        }

        logger.LogInformation("Relaying to {Host}:{Port}.", host, port);
        var stream = tcp.GetStream();
        using var done = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var up = PumpUpAsync(socket, stream, done.Token);
        var down = PumpDownAsync(stream, socket, done.Token);
        var first = await Task.WhenAny(up, down);
        await done.CancelAsync();
        try
        {
            await Task.WhenAll(up, down);
        }
        catch (Exception e) when (e is OperationCanceledException or IOException or WebSocketException)
        {
            // the other side went away
        }

        // answer the client's close (or start one when the game server hung up), so neither side sees it as dropped
        if (socket.State == WebSocketState.CloseReceived)
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        else if (socket.State == WebSocketState.Open)
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, first == down ? "game server closed" : "", CancellationToken.None);
        logger.LogInformation("Relay to {Host}:{Port} closed ({Side} ended it).", host, port, first == down ? "the game server" : "the client");
    }

    /// <summary>The client's frames, as bytes to the game server.</summary>
    private static async Task PumpUpAsync(WebSocket socket, NetworkStream stream, CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;
                await stream.WriteAsync(buffer.AsMemory(0, result.Count), ct);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>The game server's bytes, as binary frames to the client.</summary>
    private static async Task PumpDownAsync(NetworkStream stream, WebSocket socket, CancellationToken ct)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await stream.ReadAsync(buffer, ct);
                if (read == 0)
                    return;
                await socket.SendAsync(buffer.AsMemory(0, read), WebSocketMessageType.Binary, true, ct);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
