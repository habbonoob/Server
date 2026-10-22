using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Competition;

/// <summary>ForwardToASubmittableRoomMessageEvent — to one of the player's rooms that could be entered</summary>
[Incoming(ClientMessages.ForwardToASubmittableRoomMessageEvent)]
public sealed partial record ForwardToASubmittableRoomMessageEvent;
