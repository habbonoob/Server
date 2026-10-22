using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Outgoing.Users;

/// <summary>318 HandItemReceivedMessageComposer — somebody handed the player something to carry.</summary>
[Message(ServerMessages.HandItemReceivedMessageComposer)]
public sealed partial record HandItemReceivedMessageComposer(int GiverUserId, int HandItemType);
