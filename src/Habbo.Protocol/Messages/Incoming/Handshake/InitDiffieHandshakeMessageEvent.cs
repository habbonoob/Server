using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>2735 InitDiffieHandshake — asks for the signed prime and generator.</summary>
[Incoming(ClientMessages.InitDiffieHandshakeMessageEvent)]
public sealed partial record InitDiffieHandshakeMessageEvent;
