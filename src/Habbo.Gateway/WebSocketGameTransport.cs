using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway;

/// <summary>Lets browser clients reach the game port over WebSocket. A connection that opens with an
/// HTTP GET is upgraded (RFC 6455) and its binary messages carry the same byte stream a TCP client
/// sends — the messages need not line up with game packets. Everything else passes through untouched,
/// so TCP and WebSocket clients share one port and one connection handler.</summary>
public static class WebSocketGameTransport
{
    private const string AcceptGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
    private const int MaxRequestBytes = 8 * 1024;

    /// <summary>How long a connection has to say what it is (its first bytes, and a WebSocket's whole upgrade
    /// request): one that trickles them, or says nothing, is closed rather than held.</summary>
    private static readonly TimeSpan HeadTimeout = TimeSpan.FromSeconds(10);
    private static readonly byte[] Get = Encoding.ASCII.GetBytes("GET ");
    private static readonly byte[] EndOfHead = Encoding.ASCII.GetBytes("\r\n\r\n");

    /// <summary>Kestrel connection middleware: <c>listen.Use(next => WebSocketGameTransport.Middleware(next, logger))</c>.</summary>
    public static ConnectionDelegate Middleware(ConnectionDelegate next, ILogger logger) =>
        connection => HandleAsync(connection, next, logger);

    private static async Task HandleAsync(ConnectionContext connection, ConnectionDelegate next, ILogger logger)
    {
        var input = connection.Transport.Input;
        using var head = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionClosed);
        head.CancelAfter(HeadTimeout);
        bool? isHttp;
        string? key;
        try
        {
            isHttp = await StartsWithGetAsync(input, head.Token);
            if (isHttp != true)
            {
                head.CancelAfter(Timeout.InfiniteTimeSpan);
                await next(connection);
                return;
            }

            key = await ReadUpgradeRequestAsync(input, head.Token);
        }
        catch (OperationCanceledException) when (!connection.ConnectionClosed.IsCancellationRequested)
        {
            logger.LogInformation("Connection {ConnectionId}: nothing said within {Seconds}s; closed",
                connection.ConnectionId, HeadTimeout.TotalSeconds);
            return;
        }

        if (key is null)
        {
            await WriteAsync(connection.Transport.Output, "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
            logger.LogWarning("Connection {ConnectionId}: HTTP request that is not a WebSocket upgrade", connection.ConnectionId);
            return;
        }

        var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + AcceptGuid)));
        await WriteAsync(connection.Transport.Output,
            "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
            $"Sec-WebSocket-Accept: {accept}\r\n\r\n");
        logger.LogInformation("Connection {ConnectionId}: upgraded to WebSocket", connection.ConnectionId);

        using var socket = WebSocket.CreateFromStream(
            new DuplexPipeStream(connection.Transport),
            new WebSocketCreationOptions { IsServer = true, KeepAliveInterval = TimeSpan.FromSeconds(30) });
        await RunGameConnectionAsync(connection, socket, next);
    }

    /// <summary>True when the first bytes are "GET ", false for anything else, null if the peer left first.
    /// Nothing is consumed: the connection handler still sees every byte.</summary>
    private static async Task<bool?> StartsWithGetAsync(PipeReader input, CancellationToken ct)
    {
        while (true)
        {
            var result = await input.ReadAsync(ct);
            var buffer = result.Buffer;
            var (matches, length) = PrefixMatch(buffer);
            if (!matches || length == Get.Length)
            {
                // examined nothing, so the next read returns these bytes again straight away
                input.AdvanceTo(buffer.Start, buffer.Start);
                return matches;
            }
            input.AdvanceTo(buffer.Start, buffer.End);
            if (result.IsCompleted)
                return null;
        }
    }

    /// <summary>Whether the bytes buffered so far agree with "GET ", and how many were compared.</summary>
    private static (bool Matches, int Length) PrefixMatch(ReadOnlySequence<byte> buffer)
    {
        var length = (int)Math.Min(buffer.Length, Get.Length);
        Span<byte> head = stackalloc byte[Get.Length];
        buffer.Slice(0, length).CopyTo(head);
        return (head[..length].SequenceEqual(Get.AsSpan(0, length)), length);
    }

    /// <summary>Consumes the HTTP request head and returns its Sec-WebSocket-Key, or null when it is
    /// not a WebSocket upgrade.</summary>
    private static async Task<string?> ReadUpgradeRequestAsync(PipeReader input, CancellationToken ct)
    {
        while (true)
        {
            var result = await input.ReadAsync(ct);
            var buffer = result.Buffer;
            if (TryReadHead(buffer, out var head, out var afterHead))
            {
                input.AdvanceTo(afterHead);
                return UpgradeKey(head);
            }
            if (result.IsCompleted || buffer.Length > MaxRequestBytes)
            {
                input.AdvanceTo(buffer.End);
                return null;
            }
            input.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    private static bool TryReadHead(ReadOnlySequence<byte> buffer, out string head, out SequencePosition afterHead)
    {
        var reader = new SequenceReader<byte>(buffer);
        if (reader.TryReadTo(out ReadOnlySequence<byte> bytes, EndOfHead))
        {
            head = Encoding.ASCII.GetString(bytes);
            afterHead = reader.Position;
            return true;
        }
        head = "";
        afterHead = buffer.Start;
        return false;
    }

    private static string? UpgradeKey(string head)
    {
        string? key = null;
        var upgrade = false;
        foreach (var line in head.Split("\r\n").Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
                continue;
            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            if (name.Equals("Upgrade", StringComparison.OrdinalIgnoreCase))
                upgrade = value.Equals("websocket", StringComparison.OrdinalIgnoreCase);
            else if (name.Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                key = value;
        }
        return upgrade ? key : null;
    }

    private static async Task WriteAsync(PipeWriter output, string text) =>
        await output.WriteAsync(Encoding.ASCII.GetBytes(text));

    /// <summary>Runs the game connection handler over pipes fed from and drained into the WebSocket.</summary>
    private static async Task RunGameConnectionAsync(ConnectionContext connection, WebSocket socket, ConnectionDelegate next)
    {
        var inbound = new Pipe();
        var outbound = new Pipe();
        using var closed = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionClosed);
        var game = new DefaultConnectionContext(connection.ConnectionId,
            new DuplexPipe(inbound.Reader, outbound.Writer),
            new DuplexPipe(outbound.Reader, inbound.Writer))
        {
            RemoteEndPoint = connection.RemoteEndPoint,
            LocalEndPoint = connection.LocalEndPoint,
            ConnectionClosed = closed.Token,
        };

        var receiving = ReceiveAsync(socket, inbound.Writer, closed);
        var sending = SendAsync(socket, outbound.Reader, closed.Token);
        try
        {
            await next(game);
        }
        finally
        {
            await outbound.Writer.CompleteAsync();
            // a client that stopped reading never lets the last send finish: it is cut off rather than waited for
            if (await Task.WhenAny(sending, Task.Delay(TimeSpan.FromSeconds(2))) != sending)
                await closed.CancelAsync();
            await sending;
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
                }
                catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or IOException)
                {
                }
            }
            await closed.CancelAsync();
            await receiving;
        }
    }

    private static async Task ReceiveAsync(WebSocket socket, PipeWriter inbound, CancellationTokenSource closed)
    {
        try
        {
            while (true)
            {
                var memory = inbound.GetMemory(4096);
                var received = await socket.ReceiveAsync(memory, closed.Token);
                // the game protocol is binary; a close or a text message ends the connection
                if (received.MessageType != WebSocketMessageType.Binary)
                    break;
                inbound.Advance(received.Count);
                var flushed = await inbound.FlushAsync(closed.Token);
                if (flushed.IsCompleted)
                    break;
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or IOException)
        {
        }
        finally
        {
            // the game handler sees the end of its input, as it would a closed TCP connection
            // (not a cancellation: its read loop ends on a completed read)
            await inbound.CompleteAsync();
        }
    }

    private static async Task SendAsync(WebSocket socket, PipeReader outbound, CancellationToken ct)
    {
        try
        {
            while (true)
            {
                var result = await outbound.ReadAsync(ct);
                var buffer = result.Buffer;
                foreach (var segment in buffer)
                    await socket.SendAsync(segment, WebSocketMessageType.Binary, endOfMessage: true, ct);
                outbound.AdvanceTo(buffer.End);
                if (result.IsCompleted)
                    break;
            }
        }
        catch (Exception ex) when (ex is WebSocketException or OperationCanceledException or IOException)
        {
        }
        finally
        {
            await outbound.CompleteAsync();
        }
    }

    private sealed class DuplexPipe(PipeReader input, PipeWriter output) : IDuplexPipe
    {
        public PipeReader Input { get; } = input;
        public PipeWriter Output { get; } = output;
    }

    /// <summary>The connection's pipes as one read/write stream, for WebSocket.CreateFromStream.</summary>
    private sealed class DuplexPipeStream(IDuplexPipe pipe) : Stream
    {
        private readonly Stream _input = pipe.Input.AsStream(leaveOpen: true);
        private readonly Stream _output = pipe.Output.AsStream(leaveOpen: true);

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => _input.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => _input.ReadAsync(buffer, ct);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => _input.ReadAsync(buffer, offset, count, ct);
        public override void Write(byte[] buffer, int offset, int count) => _output.Write(buffer, offset, count);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => _output.WriteAsync(buffer, ct);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) => _output.WriteAsync(buffer, offset, count, ct);
        public override void Flush() => _output.Flush();
        public override Task FlushAsync(CancellationToken ct) => _output.FlushAsync(ct);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
