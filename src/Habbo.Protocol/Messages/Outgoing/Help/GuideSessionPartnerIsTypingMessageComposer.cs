using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>GuideSessionPartnerIsTypingMessageComposer — whether the other side is typing</summary>
[Message(ServerMessages.GuideSessionPartnerIsTypingMessageComposer)]
public sealed partial record GuideSessionPartnerIsTypingMessageComposer(bool IsTyping);
