using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>FriendListUpdateMessageEvent — the friend list, refreshed</summary>
[Incoming(ClientMessages.FriendListUpdateMessageEvent)]
public sealed partial record FriendListUpdateMessageEvent;
