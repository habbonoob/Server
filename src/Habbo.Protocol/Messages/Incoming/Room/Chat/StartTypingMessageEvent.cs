using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Chat;

/// <summary>1225 StartTyping.</summary>
[Incoming(ClientMessages.StartTypingMessageEvent)]
public sealed partial record StartTypingMessageEvent;
