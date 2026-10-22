using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.Help;

/// <summary>ChatReviewGuideDecidesOnOfferMessageEvent — take the chat review offered, or pass</summary>
[Incoming(ClientMessages.ChatReviewGuideDecidesOnOfferMessageEvent)]
public sealed partial record ChatReviewGuideDecidesOnOfferMessageEvent(bool Accept);
