using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Help;

/// <summary>ChatReviewSessionStartedMessageComposer — the chat to judge</summary>
[Message(ServerMessages.ChatReviewSessionStartedMessageComposer)]
public sealed partial record ChatReviewSessionStartedMessageComposer(int VotingTimeout, string ChatRecord);
