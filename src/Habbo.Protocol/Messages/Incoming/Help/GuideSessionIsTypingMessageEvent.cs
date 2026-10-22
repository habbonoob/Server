using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>GuideSessionIsTypingMessageEvent — whether the player is typing in the session</summary>
[Incoming(ClientMessages.GuideSessionIsTypingMessageEvent)]
public sealed partial record GuideSessionIsTypingMessageEvent(bool IsTyping);
