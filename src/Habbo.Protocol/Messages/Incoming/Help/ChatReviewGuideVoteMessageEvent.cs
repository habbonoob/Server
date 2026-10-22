using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>ChatReviewGuideVoteMessageEvent — a guardian's verdict</summary>
[Incoming(ClientMessages.ChatReviewGuideVoteMessageEvent)]
public sealed partial record ChatReviewGuideVoteMessageEvent(int Vote);
