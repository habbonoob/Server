using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>1038 CompleteDiffieHandshakeComposer — the signed server key, and the switch
/// that enables the server→client RC4 leg. Must be sent in plaintext.</summary>
[Message(ServerMessages.CompleteDiffieHandshakeComposer)]
public sealed partial record CompleteDiffieHandshakeComposer(
    string SignedServerPublicKey, bool EnableEncryption);
