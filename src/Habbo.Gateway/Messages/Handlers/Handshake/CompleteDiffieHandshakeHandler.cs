using Habbo.Protocol;
using Habbo.Protocol.Composers;
using Habbo.Protocol.IO;
using Habbo.Protocol.Messages.Outgoing.Handshake;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>764 CompleteDiffieHandshake — the client's encrypted DH public key. Derive the
/// shared secret, switch RC4 on, and answer with our signed key.
/// The only handler that reads the payload itself: the key exchange consumes the raw reader,
/// so there is no parsed shape to hand it.</summary>
[MessageHandler(ClientMessages.CompleteDiffieHandshakeMessageEvent)]
[PreCrypto]
internal sealed class CompleteDiffieHandshakeHandler : IMessageHandler
{
    public async Task<bool> HandleAsync(GatewaySessionContext context, MessageReader reader, CancellationToken ct)
    {
        if (!context.Crypto.ProcessClientPublicKey(reader))
            return false;

        // Sent PLAINTEXT: the client parses 1038 before installing its ciphers, so this
        // frame must never be RC4'd — even though the handshake is complete by now.
        await context.SendPlaintextAsync(
            new CompleteDiffieHandshakeComposer(context.Crypto.SignedServerPublicKey, EnableEncryption: true), ct);
        context.Session.Encrypted = true;
        return true;
    }
}
