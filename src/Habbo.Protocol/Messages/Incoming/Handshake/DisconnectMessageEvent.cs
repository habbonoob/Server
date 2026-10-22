using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>2780 Disconnect — the client is closing the connection.</summary>
[Incoming(ClientMessages.DisconnectMessageEvent)]
public sealed partial record DisconnectMessageEvent;
