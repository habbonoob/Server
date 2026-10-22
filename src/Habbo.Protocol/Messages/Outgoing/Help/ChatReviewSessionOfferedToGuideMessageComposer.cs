using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>ChatReviewSessionOfferedToGuideMessageComposer — a chat review is offered to a guardian</summary>
[Message(ServerMessages.ChatReviewSessionOfferedToGuideMessageComposer)]
public sealed partial record ChatReviewSessionOfferedToGuideMessageComposer(int AcceptanceTimeout);
