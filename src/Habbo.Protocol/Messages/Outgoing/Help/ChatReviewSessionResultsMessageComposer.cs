using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>ChatReviewSessionResultsMessageComposer — the verdict</summary>
[Message(ServerMessages.ChatReviewSessionResultsMessageComposer)]
public sealed partial record ChatReviewSessionResultsMessageComposer(int WinningVoteCode, int OwnVoteCode, IReadOnlyList<int> FinalStatus);
