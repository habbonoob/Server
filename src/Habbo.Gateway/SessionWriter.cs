using System.Buffers;
using System.IO.Pipelines;
using Habbo.Protocol.Crypto;
using Habbo.Protocol.Frames;
using Microsoft.Extensions.Logging;

namespace Habbo.Gateway;

/// <summary>Drains a session's outbound channel and writes frames to the pipe, in the client's release's headers.
/// The only writer of the pipe and the only user of the output cipher — so no locking needed.</summary>
public sealed class SessionWriter(Session session, ConnectionCrypto crypto, PipeWriter output, ILogger logger)
{
    /// <summary>How long one frame may take to go out: a client that stops reading (its socket buffer full) is
    /// given up on, so its connection closes instead of the writer waiting on it for ever.</summary>
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(15);

    public async Task RunAsync(CancellationToken ct)
    {
        using var flushDeadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            await foreach (var frame in session.Outbound.Reader.ReadAllAsync(ct))
            {
                // The client's release's header; a message its release lacks is not sent, since the client would
                // read the frame as whatever that id is to it.
                var message = frame.Message;
                if (!session.Headers.TryOutgoing(message.Header, out var header))
                {
                    logger.LogDebug("Connection {ConnectionId}: message {Header} has no counterpart in release {Release}, not sent",
                        session.Id, message.Header, session.Headers.Release);
                    continue;
                }

                Encode(output, header, message.PayloadFor(session.Headers.Release), frame.Encrypted ? crypto.OutputCipher : null);
                flushDeadline.CancelAfter(FlushTimeout);
                await output.FlushAsync(flushDeadline.Token);
                flushDeadline.CancelAfter(Timeout.InfiniteTimeSpan);
            }
        }
        catch (OperationCanceledException)
        {
            if (!ct.IsCancellationRequested)
                logger.LogInformation("Connection {ConnectionId} stopped reading what it is sent; closing it", session.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Connection {ConnectionId}: writer failed", session.Id);
        }
        finally
        {
            session.Close();
        }
    }

    /// <summary>Encodes one queued frame as the native release reads it; the cipher only covers frames queued after
    /// the handshake.</summary>
    public static void Encode(IBufferWriter<byte> output, OutboundFrame frame, StreamCipher? cipher) =>
        Encode(output, frame.Message.Header, frame.Message.Payload, frame.Encrypted ? cipher : null);

    /// <summary>Encodes one frame; the cipher, when given, covers it.</summary>
    public static void Encode(IBufferWriter<byte> output, ushort header, byte[] payload, StreamCipher? cipher)
    {
        var buffer = new ArrayBufferWriter<byte>();
        FrameEncoder.Encode(buffer, header, payload, cipher);
        output.Write(buffer.WrittenSpan);
    }
}
