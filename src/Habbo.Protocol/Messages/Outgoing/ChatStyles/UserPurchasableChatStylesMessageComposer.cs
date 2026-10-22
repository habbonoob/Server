using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.ChatStyles;

/// <summary>889 UserPurchasableChatStylesMessageComposer — the chat bubbles the player has bought.</summary>
[Message(ServerMessages.UserPurchasableChatStylesMessageComposer)]
public sealed partial record UserPurchasableChatStylesMessageComposer(IReadOnlyList<int> ChatStyleIds);
