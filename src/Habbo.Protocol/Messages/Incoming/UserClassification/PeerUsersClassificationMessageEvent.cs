using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserClassification;

/// <summary>3528 PeerUsersClassification.</summary>
[Incoming(ClientMessages.PeerUsersClassificationMessageEvent)]
public sealed partial record PeerUsersClassificationMessageEvent(string Classification);
