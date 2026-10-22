using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Habbo.Gateway.Tests;

public class WebSocketGameTransportTests
{
    /// <summary>A connection as Kestrel hands it over: the test plays the peer on the other ends of the pipes.</summary>
    private sealed class FakeConnection
    {
        public readonly Pipe FromPeer = new();
        public readonly Pipe ToPeer = new();
        public ConnectionContext Context { get; }

        public FakeConnection()
        {
            Context = new DefaultConnectionContext("test",
                new Duplex(FromPeer.Reader, ToPeer.Writer),
                new Duplex(ToPeer.Reader, FromPeer.Writer));
        }

        private sealed class Duplex(PipeReader input, PipeWriter output) : IDuplexPipe
        {
            public PipeReader Input { get; } = input;
            public PipeWriter Output { get; } = output;
        }
    }

    /// <summary>A game handler that sends back every byte it receives, until its input ends.</summary>
    private static async Task Echo(ConnectionContext connection)
    {
        var input = connection.Transport.Input;
        while (true)
        {
            var result = await input.ReadAsync();
            foreach (var segment in result.Buffer)
                await connection.Transport.Output.WriteAsync(segment);
            input.AdvanceTo(result.Buffer.End);
            if (result.IsCompleted)
                break;
        }
    }

    [Fact]
    public async Task TcpBytesReachTheHandlerUnchanged()
    {
        var connection = new FakeConnection();
        var received = new List<byte>();
        var handler = WebSocketGameTransport.Middleware(async context =>
        {
            var input = context.Transport.Input;
            while (true)
            {
                var result = await input.ReadAsync();
                received.AddRange(result.Buffer.ToArray());
                input.AdvanceTo(result.Buffer.End);
                if (result.IsCompleted)
                    break;
            }
        }, NullLogger.Instance);

        var run = handler(connection.Context);
        // a game packet: length, header, payload — the first bytes are not "GET "
        byte[] packet = [0, 0, 0, 6, 0x0F, 0xA0, 1, 2, 3, 4];
        await connection.FromPeer.Writer.WriteAsync(packet.AsMemory(0, 2));
        await connection.FromPeer.Writer.WriteAsync(packet.AsMemory(2));
        await connection.FromPeer.Writer.CompleteAsync();
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(packet, received.ToArray());
    }

    [Fact]
    public async Task WebSocketClientsGetTheSameByteStream()
    {
        var connection = new FakeConnection();
        var run = WebSocketGameTransport.Middleware(Echo, NullLogger.Instance)(connection.Context);

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        await connection.FromPeer.Writer.WriteAsync(Encoding.ASCII.GetBytes(
            "GET / HTTP/1.1\r\nHost: 127.0.0.1:30000\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
            $"Sec-WebSocket-Key: {key}\r\nSec-WebSocket-Version: 13\r\n\r\n"));

        var response = await ReadResponseHeadAsync(connection.ToPeer.Reader);
        var expected = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
        Assert.StartsWith("HTTP/1.1 101", response);
        Assert.Contains($"Sec-WebSocket-Accept: {expected}", response);

        using var client = WebSocket.CreateFromStream(
            new PeerStream(connection.ToPeer.Reader, connection.FromPeer.Writer),
            new WebSocketCreationOptions { IsServer = false });

        // one packet split over two messages comes back as the same bytes
        byte[] packet = [0, 0, 0, 6, 0x0F, 0xA0, 1, 2, 3, 4];
        await client.SendAsync(packet.AsMemory(0, 3), WebSocketMessageType.Binary, true, default);
        await client.SendAsync(packet.AsMemory(3), WebSocketMessageType.Binary, true, default);

        var echoed = new List<byte>();
        var buffer = new byte[64];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (echoed.Count < packet.Length)
        {
            var result = await client.ReceiveAsync(buffer, timeout.Token);
            Assert.Equal(WebSocketMessageType.Binary, result.MessageType);
            echoed.AddRange(buffer.AsSpan(0, result.Count).ToArray());
        }
        Assert.Equal(packet, echoed.ToArray());

        // closing the WebSocket ends the game connection
        await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, timeout.Token);
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AFrameLargerThanAPipeSegmentArrivesWhole()
    {
        // the game handler consumes nothing until a whole frame is in, as the real one does: the pipe is held back by
        // what it has examined, not consumed, so a frame past the pipe's 64 KB pause still arrives
        var length = Habbo.Protocol.Frames.FrameDecoder.MaxFrameLength;
        var frameIn = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new FakeConnection();
        var run = WebSocketGameTransport.Middleware(async context =>
        {
            var input = context.Transport.Input;
            while (true)
            {
                var result = await input.ReadAsync();
                if (result.Buffer.Length >= 4 + length || result.IsCompleted)
                {
                    frameIn.TrySetResult(result.Buffer.Length);
                    input.AdvanceTo(result.Buffer.End);
                    return;
                }
                input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            }
        }, NullLogger.Instance)(connection.Context);

        await connection.FromPeer.Writer.WriteAsync(Encoding.ASCII.GetBytes(
            "GET / HTTP/1.1\r\nHost: x\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
            "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n"));
        await ReadResponseHeadAsync(connection.ToPeer.Reader);

        using var client = WebSocket.CreateFromStream(
            new PeerStream(connection.ToPeer.Reader, connection.FromPeer.Writer),
            new WebSocketCreationOptions { IsServer = false });
        var frame = new byte[4 + length];
        frame[1] = (byte)(length >> 16);
        frame[2] = (byte)(length >> 8);
        frame[3] = (byte)length;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await client.SendAsync(frame, WebSocketMessageType.Binary, true, timeout.Token);

        Assert.Equal(frame.Length, await frameIn.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandshakeMatchesTheRfcExample()
    {
        // RFC 6455 section 1.3: this key must be answered with exactly this accept value
        var connection = new FakeConnection();
        _ = WebSocketGameTransport.Middleware(Echo, NullLogger.Instance)(connection.Context);
        await connection.FromPeer.Writer.WriteAsync(Encoding.ASCII.GetBytes(
            "GET /chat HTTP/1.1\r\nHost: server.example.com\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
            "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n"));

        Assert.Contains("Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", await ReadResponseHeadAsync(connection.ToPeer.Reader));
        await connection.FromPeer.Writer.CompleteAsync();
    }

    [Fact]
    public async Task PlainHttpRequestsAreRefused()
    {
        var connection = new FakeConnection();
        var reached = false;
        var run = WebSocketGameTransport.Middleware(_ => { reached = true; return Task.CompletedTask; }, NullLogger.Instance)(connection.Context);
        await connection.FromPeer.Writer.WriteAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: x\r\n\r\n"));
        await run.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(reached);
        Assert.StartsWith("HTTP/1.1 400", await ReadResponseHeadAsync(connection.ToPeer.Reader));
    }

    private static async Task<string> ReadResponseHeadAsync(PipeReader reader)
    {
        var end = Encoding.ASCII.GetBytes("\r\n\r\n");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            var result = await reader.ReadAsync(timeout.Token);
            var head = TryHead(result.Buffer, end, out var consumed);
            if (head != null)
            {
                reader.AdvanceTo(consumed);
                return head;
            }
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
        }
    }

    private static string? TryHead(ReadOnlySequence<byte> buffer, byte[] end, out SequencePosition consumed)
    {
        var sequence = new SequenceReader<byte>(buffer);
        if (sequence.TryReadTo(out ReadOnlySequence<byte> bytes, end))
        {
            consumed = sequence.Position;
            return Encoding.ASCII.GetString(bytes);
        }
        consumed = buffer.Start;
        return null;
    }

    private sealed class PeerStream(PipeReader input, PipeWriter output) : Stream
    {
        private readonly Stream _input = input.AsStream(leaveOpen: true);
        private readonly Stream _output = output.AsStream(leaveOpen: true);
        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => _input.Read(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => _input.ReadAsync(buffer, ct);
        public override void Write(byte[] buffer, int offset, int count) => _output.Write(buffer, offset, count);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => _output.WriteAsync(buffer, ct);
        public override void Flush() => _output.Flush();
        public override Task FlushAsync(CancellationToken ct) => _output.FlushAsync(ct);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
