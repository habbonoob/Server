using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>1824 Pong — the reply to the server's ping.</summary>
[Incoming(ClientMessages.PongMessageEvent)]
public sealed partial record PongMessageEvent;
