using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>2669 RequestFriend — ask somebody to be friends, naming them.</summary>
[Incoming(ClientMessages.RequestFriendMessageEvent)]
public sealed partial record RequestFriendMessageEvent(string Username);
