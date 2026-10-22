using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Room.Chat;

/// <summary>240 CancelTyping.</summary>
[Incoming(ClientMessages.CancelTypingMessageEvent)]
public sealed partial record CancelTypingMessageEvent;
