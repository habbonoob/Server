using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.UserClassification;

/// <summary>501 RoomUsersClassification — what kinds of user are in the room.</summary>
[Incoming(ClientMessages.RoomUsersClassificationMessageEvent)]
public sealed partial record RoomUsersClassificationMessageEvent(string Classification);
