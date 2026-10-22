using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>ChatReviewSessionDetachedMessageComposer — the guardian is out of the review</summary>
[Message(ServerMessages.ChatReviewSessionDetachedMessageComposer)]
public sealed partial record ChatReviewSessionDetachedMessageComposer;
