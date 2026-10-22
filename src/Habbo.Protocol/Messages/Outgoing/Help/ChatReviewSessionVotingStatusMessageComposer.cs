using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>ChatReviewSessionVotingStatusMessageComposer — each guardian's vote so far</summary>
[Message(ServerMessages.ChatReviewSessionVotingStatusMessageComposer)]
public sealed partial record ChatReviewSessionVotingStatusMessageComposer(IReadOnlyList<int> Status);
