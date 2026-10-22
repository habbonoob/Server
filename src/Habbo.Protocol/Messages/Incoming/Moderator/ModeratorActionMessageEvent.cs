using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Moderator;

/// <summary>ModeratorActionMessageEvent — a message to everyone in the moderator's room, perhaps kicking them too</summary>
[Incoming(ClientMessages.ModeratorActionMessageEvent)]
public sealed partial record ModeratorActionMessageEvent(int ActionType, string Message, string Extra);
