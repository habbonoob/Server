using Habbo.Protocol.Wire;

namespace Habbo.Protocol.Messages.Incoming.FriendList;

/// <summary>FindNewFriendsMessageEvent — go somewhere busy to meet people</summary>
[Incoming(ClientMessages.FindNewFriendsMessageEvent)]
public sealed partial record FindNewFriendsMessageEvent;
