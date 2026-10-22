using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>4000 ClientHello — the release and flash build the client reports.</summary>
[Incoming(ClientMessages.ClientHelloMessageEvent)]
public sealed partial record ClientHelloMessageEvent(string Release, string Type);
