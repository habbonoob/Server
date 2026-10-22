using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>1142 InitDiffieHandshakeComposer — the RSA-signed prime and generator.</summary>
[Message(ServerMessages.InitDiffieHandshakeComposer)]
public sealed partial record InitDiffieHandshakeComposer(string SignedPrime, string SignedGenerator);
