using System.Buffers;
using System.IO.Pipelines;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;

namespace Habbo.Gateway;

/// <summary>The per-connection view handed to message handlers: session state, crypto, and sends.
/// Handlers never touch the socket directly — sends enqueue on the session's outbound channel.</summary>
public sealed class GatewaySessionContext
{
    public required Session Session { get; init; }
    public required ConnectionCrypto Crypto { get; init; }
    public required PipeWriter Output { get; init; }

    /// <summary>Enqueues a composed message. Encrypted once the handshake is complete.</summary>
    public Task SendAsync(IComposer composer, CancellationToken ct)
        => Enqueue(composer, Crypto.Complete);

    /// <summary>Explicitly plaintext — for the handshake frames (1142, 1038): the client
    /// parses both before installing its ciphers, so they must never be RC4'd.</summary>
    public Task SendPlaintextAsync(IComposer composer, CancellationToken ct)
        => Enqueue(composer, encrypted: false);

    private Task Enqueue(IComposer composer, bool encrypted)
    {
        // The session may be closing; nothing sensible to do with the frame then.
        Session.Enqueue(new OutboundFrame(composer.ToMessage(), encrypted));
        return Task.CompletedTask;
    }
}
