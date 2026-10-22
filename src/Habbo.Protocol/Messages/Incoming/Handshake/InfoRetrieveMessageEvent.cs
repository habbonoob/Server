using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Handshake;

/// <summary>1484 InfoRetrieve — asks for the player's own account data.</summary>
[Incoming(ClientMessages.InfoRetrieveMessageEvent)]
public sealed partial record InfoRetrieveMessageEvent;
