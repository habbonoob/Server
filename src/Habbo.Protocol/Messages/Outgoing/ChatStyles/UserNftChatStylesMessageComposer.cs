using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.ChatStyles;

/// <summary>1581 UserNftChatStylesMessageComposer — the chat bubbles the player's NFTs give them.</summary>
[Message(ServerMessages.UserNftChatStylesMessageComposer)]
public sealed partial record UserNftChatStylesMessageComposer(IReadOnlyList<int> ChatStyleIds);
