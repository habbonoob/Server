using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>ChatReviewGuideDetachedMessageEvent — a guardian leaves the review</summary>
[Incoming(ClientMessages.ChatReviewGuideDetachedMessageEvent)]
public sealed partial record ChatReviewGuideDetachedMessageEvent;
