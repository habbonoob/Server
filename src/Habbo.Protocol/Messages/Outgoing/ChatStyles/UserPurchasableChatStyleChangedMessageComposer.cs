using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.ChatStyles;

/// <summary>2223 UserPurchasableChatStyleChangedMessageComposer.</summary>
[Message(ServerMessages.UserPurchasableChatStyleChangedMessageComposer)]
public sealed partial record UserPurchasableChatStyleChangedMessageComposer(bool Added, int StyleId);
