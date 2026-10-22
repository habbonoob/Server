using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Handshake;

/// <summary>PingMessageComposer — is the client still there; it answers with a pong</summary>
[Message(ServerMessages.PingMessageComposer)]
public sealed partial record PingMessageComposer;
