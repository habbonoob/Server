using Habbo.Protocol.Composers;
using Habbo.Protocol.Messages.Incoming.Handshake;
using Habbo.Protocol.Messages.Outgoing.Handshake;

namespace Habbo.Gateway.Messages.Handlers.Handshake;

/// <summary>2735 InitDiffieHandshake — answer with the RSA-signed prime and generator.</summary>
[PreCrypto]
internal sealed class InitDiffieHandshakeHandler : IMessageHandler<InitDiffieHandshakeMessageEvent>
{
    public async Task<bool> HandleAsync(
        GatewaySessionContext context, InitDiffieHandshakeMessageEvent message, CancellationToken ct)
    {
        // asked once per connection; asking again is a client wasting the server's signatures
        if (!context.Crypto.BeginHandshake())
            return false;

        await context.SendPlaintextAsync(
            new InitDiffieHandshakeComposer(context.Crypto.SignedPrime, context.Crypto.SignedGenerator), ct);
        return true;
    }
}
